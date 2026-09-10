using System.Security.Claims;

namespace EasyPanel.Backend.Infrastructure.Security;

public interface IServerPermissionChecker
{
    Task<bool> HasPermissionAsync(ClaimsPrincipal user, Guid instanceId, ServerPermissionKind kind, CancellationToken cancellationToken);
}
