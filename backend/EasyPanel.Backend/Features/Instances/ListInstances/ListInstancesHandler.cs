using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EasyPanel.Backend.Infrastructure;
using EasyPanel.Backend.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Features.Instances.ListInstances;

public sealed class ListInstancesHandler(AppDbContext dbContext)
{
    public async Task<IReadOnlyList<InstanceSummary>> HandleAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var query = dbContext.Instances.Include(instance => instance.Node).AsQueryable();

        if (!user.IsInRole(nameof(UserRole.Admin)))
        {
            var userId = Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            var permittedInstanceIds = dbContext.StaffServerPermissions
                .Where(permission => permission.UserId == userId)
                .Select(permission => permission.InstanceId);

            query = query.Where(instance => permittedInstanceIds.Contains(instance.Id));
        }

        return await query
            .OrderBy(instance => instance.DisplayName)
            .Select(instance => new InstanceSummary(instance.Id, instance.DisplayName, instance.NodeId, instance.Node!.DisplayName, instance.Status))
            .ToListAsync(cancellationToken);
    }
}
