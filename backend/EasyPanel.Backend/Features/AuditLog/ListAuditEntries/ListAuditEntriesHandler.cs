using EasyPanel.Backend.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Features.AuditLog.ListAuditEntries;

public sealed class ListAuditEntriesHandler(AppDbContext dbContext)
{
    private const int MaxPageSize = 200;

    public async Task<IReadOnlyList<AuditLogEntrySummary>> HandleAsync(Guid? instanceId, int skip, int take, CancellationToken cancellationToken)
    {
        var query = dbContext.AuditLogEntries.AsQueryable();

        if (instanceId is not null)
        {
            query = query.Where(entry => entry.InstanceId == instanceId);
        }

        return await query
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .Skip(Math.Max(skip, 0))
            .Take(Math.Clamp(take, 1, MaxPageSize))
            .Select(entry => new AuditLogEntrySummary(entry.Id, entry.ActorUserId, entry.InstanceId, entry.NodeId, entry.Action, entry.DetailsJson, entry.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
