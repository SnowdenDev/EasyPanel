using System.Security.Claims;

namespace EasyPanel.Backend.Features.Instances.ListInstances;

public static class ListInstancesEndpoint
{
    public static void MapListInstancesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/instances", async (ClaimsPrincipal user, ListInstancesHandler handler, CancellationToken cancellationToken) =>
        {
            var instances = await handler.HandleAsync(user, cancellationToken);
            return Results.Ok(instances);
        })
        .WithName("ListInstances")
        .RequireAuthorization();
    }
}
