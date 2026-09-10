using System.Security.Claims;
using EasyPanel.Backend.Infrastructure.Security;

namespace EasyPanel.Backend.Features.Instances.GetInstanceStatus;

public static class GetInstanceStatusEndpoint
{
    public static void MapGetInstanceStatusEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/instances/{instanceId:guid}/status", async (
            Guid instanceId,
            ClaimsPrincipal user,
            IServerPermissionChecker permissionChecker,
            GetInstanceStatusHandler handler,
            CancellationToken cancellationToken) =>
        {
            var canView = await permissionChecker.HasPermissionAsync(user, instanceId, ServerPermissionKind.ViewConsole, cancellationToken);
            if (!canView)
            {
                return Results.Forbid();
            }

            var details = await handler.HandleAsync(instanceId, cancellationToken);

            return details is null
                ? Results.NotFound()
                : Results.Ok(details);
        })
        .WithName("GetInstanceStatus")
        .RequireAuthorization();
    }
}
