using EasyPanel.Backend.Infrastructure;
using EasyPanel.Backend.Infrastructure.Entities;
using EasyPanel.Contracts.Control.BackendToDaemon;
using EasyPanel.Contracts.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Features.Instances.StopInstance;

public sealed class StopInstanceHandler(AppDbContext dbContext, IHubContext<DaemonControlHub> daemonControlHub)
{
    private const int DefaultGracePeriodSeconds = 30;

    public async Task<StopInstanceResult> HandleAsync(Guid instanceId, Guid actorUserId, bool force, CancellationToken cancellationToken)
    {
        var instance = await dbContext.Instances
            .Include(candidate => candidate.Node)
            .SingleOrDefaultAsync(candidate => candidate.Id == instanceId, cancellationToken);

        if (instance is null)
        {
            return new StopInstanceResult(false, $"No instance with id '{instanceId}' exists.");
        }

        if (instance.Node is null || !instance.Node.IsOnline)
        {
            return new StopInstanceResult(false, "Node is offline — action was not sent.");
        }

        var nodeGroup = HubGroupNames.NodeGroup(instance.NodeId);

        if (force)
        {
            // KillInstance bypasses the grace period entirely — same daemon-side effect as
            // StopInstance with GracePeriodSeconds=0, but as its own named hub method so the
            // protocol documents the two as distinct actions (see docs/architecture.md).
            await daemonControlHub.Clients.Group(nodeGroup).SendAsync("KillInstance", instance.Id, cancellationToken);
        }
        else
        {
            var command = new StopInstanceCommand(instance.Id, DefaultGracePeriodSeconds);
            await daemonControlHub.Clients.Group(nodeGroup).SendAsync("StopInstance", command, cancellationToken);
        }

        instance.Status = InstanceStatus.Stopping;
        instance.UpdatedAtUtc = DateTimeOffset.UtcNow;

        dbContext.AuditLogEntries.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            InstanceId = instance.Id,
            NodeId = instance.NodeId,
            Action = force ? "InstanceKillRequested" : "InstanceStopRequested",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new StopInstanceResult(true, null);
    }
}
