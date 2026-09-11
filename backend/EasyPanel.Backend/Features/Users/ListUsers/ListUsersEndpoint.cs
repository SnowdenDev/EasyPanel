using EasyPanel.Backend.Infrastructure.Entities;

namespace EasyPanel.Backend.Features.Users.ListUsers;

public static class ListUsersEndpoint
{
    public static void MapListUsersEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users", async (ListUsersHandler handler, CancellationToken cancellationToken) =>
        {
            var users = await handler.HandleAsync(cancellationToken);
            return Results.Ok(users);
        })
        .WithName("ListUsers")
        .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)));
    }
}
