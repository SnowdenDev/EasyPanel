using EasyPanel.Backend.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Features.Instances.GetInstanceStatus;

public sealed class GetInstanceStatusHandler(AppDbContext dbContext)
{
    public async Task<InstanceStatusDetails?> HandleAsync(Guid instanceId, CancellationToken cancellationToken)
    {
        return await dbContext.Instances
            .Where(instance => instance.Id == instanceId)
            .Select(instance => new InstanceStatusDetails(
                instance.Id,
                instance.DisplayName,
                instance.Status,
                instance.NodeId,
                instance.Node!.IsOnline,
                instance.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
