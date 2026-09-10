using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EasyPanel.Backend.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Infrastructure.Security;

/// <summary>
/// Admin always passes — this table only ever constrains the Staff role
/// (see StaffServerPermission).
/// </summary>
public sealed class ServerPermissionChecker(AppDbContext dbContext) : IServerPermissionChecker
{
    public async Task<bool> HasPermissionAsync(ClaimsPrincipal user, Guid instanceId, ServerPermissionKind kind, CancellationToken cancellationToken)
    {
        if (user.IsInRole(nameof(UserRole.Admin)))
        {
            return true;
        }

        var userId = Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

        var permission = await dbContext.StaffServerPermissions
            .SingleOrDefaultAsync(candidate => candidate.UserId == userId && candidate.InstanceId == instanceId, cancellationToken);

        if (permission is null)
        {
            return false;
        }

        return kind switch
        {
            ServerPermissionKind.ViewConsole => permission.CanViewConsole,
            ServerPermissionKind.SendConsoleInput => permission.CanSendConsoleInput,
            ServerPermissionKind.ControlPower => permission.CanControlPower,
            ServerPermissionKind.AccessFileManager => permission.CanAccessFileManager,
            ServerPermissionKind.EditSettings => permission.CanEditSettings,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }
}
