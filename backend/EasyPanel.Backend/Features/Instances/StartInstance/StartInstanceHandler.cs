using System.Text.Json;
using EasyPanel.Backend.Infrastructure;
using EasyPanel.Backend.Infrastructure.Entities;
using EasyPanel.Contracts.Control.BackendToDaemon;
using EasyPanel.Contracts.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Features.Instances.StartInstance;

public sealed class StartInstanceHandler(AppDbContext dbContext, IHubContext<DaemonControlHub> daemonControlHub)
{
    public async Task<StartInstanceResult> HandleAsync(Guid instanceId, Guid actorUserId, CancellationToken cancellationToken)
    {
        var instance = await dbContext.Instances
            .Include(candidate => candidate.Node)
            .SingleOrDefaultAsync(candidate => candidate.Id == instanceId, cancellationToken);

        if (instance is null)
        {
            return new StartInstanceResult(false, $"No instance with id '{instanceId}' exists.");
        }

        if (instance.Node is null || !instance.Node.IsOnline)
        {
            // Reject immediately rather than queue — see the reconnect/backoff design in
            // docs/architecture.md. A durable offline-command outbox is deferred to v2.
            return new StartInstanceResult(false, "Node is offline — action was not sent.");
        }

        var environmentVariables = string.IsNullOrEmpty(instance.EnvironmentVariablesJson)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(instance.EnvironmentVariablesJson) ?? new Dictionary<string, string>();

        var command = new LaunchInstanceCommand(
            instance.Id,
            instance.WorkDirectory,
            instance.ExecutableRelativePath,
            instance.ExpectedExecutableSha256,
            instance.LaunchArguments,
            environmentVariables,
            instance.CpuLimitPercent,
            instance.MemoryLimitMegabytes,
            instance.AutoRestartEnabled);

        await daemonControlHub.Clients.Group(HubGroupNames.NodeGroup(instance.NodeId)).SendAsync("LaunchInstance", command, cancellationToken);

        instance.Status = InstanceStatus.Starting;
        instance.UpdatedAtUtc = DateTimeOffset.UtcNow;

        dbContext.AuditLogEntries.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            InstanceId = instance.Id,
            NodeId = instance.NodeId,
            Action = "InstanceStartRequested",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new StartInstanceResult(true, null);
    }
}
