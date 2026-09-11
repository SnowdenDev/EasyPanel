using EasyPanel.Backend.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Features.Staff.GetStaffPermissions;

public sealed class GetStaffPermissionsHandler(AppDbContext dbContext)
{
    public async Task<GetStaffPermissionsResult> HandleAsync(Guid targetUserId, CancellationToken cancellationToken)
    {
        var targetUserExists = await dbContext.Users.AnyAsync(user => user.Id == targetUserId, cancellationToken);
        if (!targetUserExists)
        {
            return new GetStaffPermissionsResult(false, $"No user with id '{targetUserId}' exists.", null);
        }

        var grants = await dbContext.StaffServerPermissions
            .Where(permission => permission.UserId == targetUserId)
            .ToDictionaryAsync(permission => permission.InstanceId, cancellationToken);

        var instances = await dbContext.Instances
            .OrderBy(instance => instance.DisplayName)
            .Select(instance => new { instance.Id, instance.DisplayName })
            .ToListAsync(cancellationToken);

        // Every instance shows up here, even ones with no grant yet — the matrix is
        // "everything this user could be given access to," not just what's already set.
        var permissions = instances.Select(instance =>
        {
            grants.TryGetValue(instance.Id, out var grant);
            return new InstancePermissionSummary(
                instance.Id,
                instance.DisplayName,
                grant?.CanViewConsole ?? false,
                grant?.CanSendConsoleInput ?? false,
                grant?.CanControlPower ?? false,
                grant?.CanAccessFileManager ?? false,
                grant?.CanEditSettings ?? false);
        }).ToList();

        return new GetStaffPermissionsResult(true, null, permissions);
    }
}
