using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EasyPanel.Backend.Infrastructure.Entities;

namespace EasyPanel.Backend.Features.Staff.AssignServerPermissions;

public static class AssignServerPermissionsEndpoint
{
    public static void MapAssignServerPermissionsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/staff/{targetUserId:guid}/permissions/{instanceId:guid}", async (
            Guid targetUserId,
            Guid instanceId,
            AssignServerPermissionsRequest request,
            ClaimsPrincipal user,
            AssignServerPermissionsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var actorUserId = Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            var result = await handler.HandleAsync(targetUserId, instanceId, request, actorUserId, cancellationToken);

            return result.Succeeded
                ? Results.NoContent()
                : Results.BadRequest(new { message = result.ErrorMessage });
        })
        .WithName("AssignServerPermissions")
        .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)));
    }
}
