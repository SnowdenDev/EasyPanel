using System.Text.Json;
using EasyPanel.Backend.Infrastructure;
using EasyPanel.Backend.Infrastructure.Entities;
using EasyPanel.Contracts.Enums;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Features.Nodes.DeleteNode;

public sealed class DeleteNodeHandler(AppDbContext dbContext)
{
    public async Task<DeleteNodeResult> HandleAsync(Guid nodeId, Guid actorUserId, CancellationToken cancellationToken)
    {
        var node = await dbContext.Nodes.SingleOrDefaultAsync(candidate => candidate.Id == nodeId, cancellationToken);
        if (node is null)
        {
            return new DeleteNodeResult(false, $"No node with id '{nodeId}' exists.", NotFound: true);
        }

        var instances = await dbContext.Instances.Where(instance => instance.NodeId == nodeId).ToListAsync(cancellationToken);

        // Deleting a node that still has a running instance would orphan the real
        // process on that machine — the daemon has no idea the panel forgot about it.
        if (instances.Any(instance => instance.Status is InstanceStatus.Running or InstanceStatus.Starting or InstanceStatus.Stopping))
        {
            return new DeleteNodeResult(false, "Stop every instance on this node before deleting it.");
        }

        var displayName = node.DisplayName;
        var instanceIds = instances.Select(instance => instance.Id).ToList();

        var permissions = dbContext.StaffServerPermissions.Where(permission => instanceIds.Contains(permission.InstanceId));
        dbContext.StaffServerPermissions.RemoveRange(permissions);

        var portAllocations = dbContext.PortAllocations.Where(allocation => allocation.NodeId == nodeId);
        dbContext.PortAllocations.RemoveRange(portAllocations);

        // Instance rows cascade-delete with the node at the database level (FK configured
        // with DeleteBehavior.Cascade) — no need to remove them here explicitly.
        dbContext.Nodes.Remove(node);

        dbContext.AuditLogEntries.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            InstanceId = null,
            NodeId = null,
            Action = "NodeDeleted",
            DetailsJson = JsonSerializer.Serialize(new { displayName }),
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new DeleteNodeResult(true, null);
    }
}
