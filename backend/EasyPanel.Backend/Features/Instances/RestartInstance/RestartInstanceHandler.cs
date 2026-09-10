using EasyPanel.Backend.Infrastructure;
using EasyPanel.Backend.Infrastructure.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Features.Instances.RestartInstance;

public sealed class RestartInstanceHandler(AppDbContext dbContext, IHubContext<DaemonControlHub> daemonControlHub)
{
    public async Task<RestartInstanceResult> HandleAsync(Guid instanceId, Guid actorUserId, CancellationToken cancellationToken)
    {
        var instance = await dbContext.Instances
            .Include(candidate => candidate.Node)
            .SingleOrDefaultAsync(candidate => candidate.Id == instanceId, cancellationToken);

        if (instance is null)
        {
            return new RestartInstanceResult(false, $"No instance with id '{instanceId}' exists.");
        }

        if (instance.Node is null || !instance.Node.IsOnline)
        {
            return new RestartInstanceResult(false, "Node is offline — action was not sent.");
        }

        await daemonControlHub.Clients.Group(HubGroupNames.NodeGroup(instance.NodeId))
            .SendAsync("RestartInstance", instance.Id, cancellationToken);

        dbContext.AuditLogEntries.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            InstanceId = instance.Id,
            NodeId = instance.NodeId,
            Action = "InstanceRestartRequested",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new RestartInstanceResult(true, null);
    }
}
