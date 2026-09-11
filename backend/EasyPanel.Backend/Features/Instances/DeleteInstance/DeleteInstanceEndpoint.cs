using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EasyPanel.Backend.Infrastructure.Security;

namespace EasyPanel.Backend.Features.Instances.DeleteInstance;

public static class DeleteInstanceEndpoint
{
    public static void MapDeleteInstanceEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/instances/{instanceId:guid}", async (
            Guid instanceId,
            ClaimsPrincipal user,
            IServerPermissionChecker permissionChecker,
            DeleteInstanceHandler handler,
            CancellationToken cancellationToken) =>
        {
            var canEditSettings = await permissionChecker.HasPermissionAsync(user, instanceId, ServerPermissionKind.EditSettings, cancellationToken);
            if (!canEditSettings)
            {
                return Results.Forbid();
            }

            var actorUserId = Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            var result = await handler.HandleAsync(instanceId, actorUserId, cancellationToken);

            if (result.Succeeded)
            {
                return Results.NoContent();
            }

            return result.NotFound
                ? Results.NotFound(new { message = result.ErrorMessage })
                : Results.Conflict(new { message = result.ErrorMessage });
        })
        .WithName("DeleteInstance")
        .RequireAuthorization();
    }
}
