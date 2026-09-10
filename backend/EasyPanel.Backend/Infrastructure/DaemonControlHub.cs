using EasyPanel.Backend.Infrastructure.Entities;
using EasyPanel.Backend.Infrastructure.Security;
using EasyPanel.Contracts.Control.DaemonToBackend;
using EasyPanel.Contracts.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Infrastructure;

/// <summary>
/// The daemon's main control channel — one persistent outbound connection per node.
/// Authenticated by node token, never by user JWT (see NodeTokenAuthenticationHandler).
/// Kept separate from DashboardHub so a bug can never let a dashboard JWT reach these
/// daemon-only methods.
/// </summary>
[Authorize(AuthenticationSchemes = NodeTokenAuthenticationDefaults.SchemeName)]
public sealed class DaemonControlHub(
    AppDbContext dbContext,
    INodeConnectionTracker connectionTracker,
    IHubContext<DashboardHub> dashboardHub,
    PendingHashComputationTracker hashComputationTracker) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var nodeId = Context.GetNodeId();

        var previousConnectionId = connectionTracker.SetActiveConnection(nodeId, Context.ConnectionId);
        if (previousConnectionId is not null)
        {
            // Ask the stale connection to stop itself rather than severing its transport
            // directly — SignalR doesn't expose a public way to abort an arbitrary other
            // connection from here, and a graceful "please disconnect" is enough since
            // there should only ever be one live daemon process per node in practice.
            await Clients.Client(previousConnectionId).SendAsync("ForceDisconnect", "Replaced by a newer connection for this node.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroupNames.NodeGroup(nodeId));

        var node = await dbContext.Nodes.SingleAsync(candidate => candidate.Id == nodeId);
        node.IsOnline = true;
        node.LastHeartbeatAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        await dashboardHub.Clients.All.SendAsync("NodeConnectivityChanged", nodeId, true);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var nodeId = Context.GetNodeId();
        var wasStillActiveConnection = connectionTracker.RemoveIfCurrent(nodeId, Context.ConnectionId);

        if (wasStillActiveConnection)
        {
            var node = await dbContext.Nodes.SingleAsync(candidate => candidate.Id == nodeId);
            node.IsOnline = false;
            await dbContext.SaveChangesAsync();

            // Not "Stopped" — the daemon may still be running these instances, we've just
            // lost the connection to it. ReportCurrentInstanceStates reconciles the real
            // state once the daemon reconnects (see docs/architecture.md).
            var affectedInstances = await dbContext.Instances
                .Where(instance => instance.NodeId == nodeId
                    && (instance.Status == InstanceStatus.Running || instance.Status == InstanceStatus.Starting))
                .ToListAsync();

            foreach (var instance in affectedInstances)
            {
                instance.Status = InstanceStatus.Unknown;
            }

            await dbContext.SaveChangesAsync();

            await dashboardHub.Clients.All.SendAsync("NodeConnectivityChanged", nodeId, false);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task ReportHeartbeat(DaemonHeartbeat heartbeat)
    {
        var node = await dbContext.Nodes.SingleAsync(candidate => candidate.Id == heartbeat.NodeId);
        node.LastHeartbeatAtUtc = heartbeat.TimestampUtc;
        node.DaemonVersion = heartbeat.DaemonVersion;
        await dbContext.SaveChangesAsync();
    }

    public async Task ReportInstanceStatusChanged(InstanceStatusChanged statusChanged)
    {
        var instance = await dbContext.Instances.SingleAsync(candidate => candidate.Id == statusChanged.InstanceId);
        instance.Status = statusChanged.NewStatus;
        instance.UpdatedAtUtc = statusChanged.TimestampUtc;
        await dbContext.SaveChangesAsync();

        await dashboardHub.Clients.Group(HubGroupNames.InstanceConsoleGroup(statusChanged.InstanceId))
            .SendAsync("InstanceStatusChanged", statusChanged.InstanceId, statusChanged.NewStatus.ToString());
    }

    public async Task ReportConsoleOutputLine(ConsoleOutputLine line)
    {
        // A small backfill ring buffer for late-subscribing dashboard clients is a
        // deliberate Phase 3 addition, tracked in docs/architecture.md — not needed for
        // the Phase 1/2 verification pass, which only needs live streaming to work.
        await dashboardHub.Clients.Group(HubGroupNames.InstanceConsoleGroup(line.InstanceId))
            .SendAsync("ConsoleOutputReceived", line.InstanceId, line.StreamKind.ToString(), line.Text, line.TimestampUtc);
    }

    public async Task ReportInstanceCrashed(InstanceCrashed crashed)
    {
        var instance = await dbContext.Instances.SingleAsync(candidate => candidate.Id == crashed.InstanceId);
        instance.Status = InstanceStatus.Crashed;
        instance.UpdatedAtUtc = crashed.TimestampUtc;

        dbContext.AuditLogEntries.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            InstanceId = crashed.InstanceId,
            Action = "InstanceCrashed",
            DetailsJson = System.Text.Json.JsonSerializer.Serialize(crashed),
            CreatedAtUtc = crashed.TimestampUtc,
        });

        await dbContext.SaveChangesAsync();

        await dashboardHub.Clients.Group(HubGroupNames.InstanceConsoleGroup(crashed.InstanceId))
            .SendAsync("InstanceStatusChanged", crashed.InstanceId, InstanceStatus.Crashed.ToString());
    }

    public async Task ReportCurrentInstanceStates(InstanceStatusSnapshot[] snapshot)
    {
        var instanceIds = snapshot.Select(entry => entry.InstanceId).ToList();
        var instances = await dbContext.Instances
            .Where(instance => instanceIds.Contains(instance.Id))
            .ToDictionaryAsync(instance => instance.Id);

        foreach (var entry in snapshot)
        {
            if (!instances.TryGetValue(entry.InstanceId, out var instance))
            {
                continue;
            }

            instance.Status = entry.CurrentStatus;
            instance.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await dashboardHub.Clients.Group(HubGroupNames.InstanceConsoleGroup(entry.InstanceId))
                .SendAsync("InstanceStatusChanged", entry.InstanceId, entry.CurrentStatus.ToString());
        }

        await dbContext.SaveChangesAsync();
    }

    public void ReportExecutableHashComputed(HashComputationResult result) => hashComputationTracker.Complete(result);
}
