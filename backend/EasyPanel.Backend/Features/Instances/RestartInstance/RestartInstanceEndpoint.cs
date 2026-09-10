using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EasyPanel.Backend.Infrastructure.Security;

namespace EasyPanel.Backend.Features.Instances.RestartInstance;

public static class RestartInstanceEndpoint
{
    public static void MapRestartInstanceEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/instances/{instanceId:guid}/restart", async (
            Guid instanceId,
            ClaimsPrincipal user,
            IServerPermissionChecker permissionChecker,
            RestartInstanceHandler handler,
            CancellationToken cancellationToken) =>
        {
            var canControlPower = await permissionChecker.HasPermissionAsync(user, instanceId, ServerPermissionKind.ControlPower, cancellationToken);
            if (!canControlPower)
            {
                return Results.Forbid();
            }

            var actorUserId = Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            var result = await handler.HandleAsync(instanceId, actorUserId, cancellationToken);

            return result.Succeeded
                ? Results.Accepted()
                : Results.Conflict(new { message = result.ErrorMessage });
        })
        .WithName("RestartInstance")
        .RequireAuthorization();
    }
}
