using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EasyPanel.Backend.Infrastructure.Security;

namespace EasyPanel.Backend.Features.Instances.StartInstance;

public static class StartInstanceEndpoint
{
    public static void MapStartInstanceEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/instances/{instanceId:guid}/start", async (
            Guid instanceId,
            ClaimsPrincipal user,
            IServerPermissionChecker permissionChecker,
            StartInstanceHandler handler,
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
        .WithName("StartInstance")
        .RequireAuthorization();
    }
}
