using EasyPanel.Backend.Infrastructure.Entities;

namespace EasyPanel.Backend.Features.Nodes.ComputeExecutableHash;

public static class ComputeExecutableHashEndpoint
{
    public static void MapComputeExecutableHashEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/nodes/{nodeId:guid}/compute-hash", async (
            Guid nodeId,
            ComputeExecutableHashRequest request,
            ComputeExecutableHashHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(nodeId, request, cancellationToken);

            return result.Succeeded
                ? Results.Ok(new { sha256Hex = result.Sha256Hex })
                : Results.BadRequest(new { message = result.ErrorMessage });
        })
        .WithName("ComputeExecutableHash")
        .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)));
    }
}
