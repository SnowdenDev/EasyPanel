using EasyPanel.Backend.Infrastructure;
using EasyPanel.Backend.Infrastructure.Entities;
using EasyPanel.Contracts.Enums;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Features.Instances.DeleteInstance;

public sealed class DeleteInstanceHandler(AppDbContext dbContext)
{
    public async Task<DeleteInstanceResult> HandleAsync(Guid instanceId, Guid actorUserId, CancellationToken cancellationToken)
    {
        var instance = await dbContext.Instances.SingleOrDefaultAsync(candidate => candidate.Id == instanceId, cancellationToken);
        if (instance is null)
        {
            return new DeleteInstanceResult(false, $"No instance with id '{instanceId}' exists.", NotFound: true);
        }

        // Deleting a running instance would orphan the real process on the node — the
        // daemon has no idea the panel forgot about it. Stop it first.
        if (instance.Status is InstanceStatus.Running or InstanceStatus.Starting or InstanceStatus.Stopping)
        {
            return new DeleteInstanceResult(false, "Stop the instance before deleting it.");
        }

        var displayName = instance.DisplayName;
        var nodeId = instance.NodeId;

        var permissions = dbContext.StaffServerPermissions.Where(permission => permission.InstanceId == instanceId);
        dbContext.StaffServerPermissions.RemoveRange(permissions);

        var portAllocations = dbContext.PortAllocations.Where(allocation => allocation.InstanceId == instanceId);
        dbContext.PortAllocations.RemoveRange(portAllocations);

        dbContext.Instances.Remove(instance);

        dbContext.AuditLogEntries.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            InstanceId = null,
            NodeId = nodeId,
            Action = "InstanceDeleted",
            DetailsJson = $"{{\"displayName\":\"{displayName}\"}}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new DeleteInstanceResult(true, null);
    }
}
