using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EasyPanel.Backend.Infrastructure.Entities;

namespace EasyPanel.Backend.Features.Nodes.DeleteNode;

public static class DeleteNodeEndpoint
{
    public static void MapDeleteNodeEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/nodes/{nodeId:guid}", async (
            Guid nodeId,
            ClaimsPrincipal user,
            DeleteNodeHandler handler,
            CancellationToken cancellationToken) =>
        {
            var actorUserId = Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            var result = await handler.HandleAsync(nodeId, actorUserId, cancellationToken);

            if (result.Succeeded)
            {
                return Results.NoContent();
            }

            return result.NotFound
                ? Results.NotFound(new { message = result.ErrorMessage })
                : Results.Conflict(new { message = result.ErrorMessage });
        })
        .WithName("DeleteNode")
        .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)));
    }
}
