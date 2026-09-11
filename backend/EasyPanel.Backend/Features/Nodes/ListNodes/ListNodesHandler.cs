using EasyPanel.Backend.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Features.Nodes.ListNodes;

public sealed class ListNodesHandler(AppDbContext dbContext)
{
    public async Task<IReadOnlyList<NodeSummary>> HandleAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Nodes
            .OrderBy(node => node.DisplayName)
            .Select(node => new NodeSummary(
                node.Id,
                node.DisplayName,
                node.ConnectivityMode,
                node.IsOnline,
                node.LastHeartbeatAtUtc,
                node.DaemonVersion,
                node.HostName,
                node.LogicalProcessorCount,
                node.TotalPhysicalMemoryMegabytes,
                node.AvailableMemoryMegabytes,
                node.CpuUsagePercent))
            .ToListAsync(cancellationToken);
    }
}
