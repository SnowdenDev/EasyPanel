using EasyPanel.Backend.Infrastructure.Entities;

namespace EasyPanel.Backend.Features.Staff.GetStaffPermissions;

public static class GetStaffPermissionsEndpoint
{
    public static void MapGetStaffPermissionsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/staff/{targetUserId:guid}/permissions", async (
            Guid targetUserId,
            GetStaffPermissionsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(targetUserId, cancellationToken);

            return result.Succeeded
                ? Results.Ok(result.Permissions)
                : Results.NotFound(new { message = result.ErrorMessage });
        })
        .WithName("GetStaffPermissions")
        .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)));
    }
}
