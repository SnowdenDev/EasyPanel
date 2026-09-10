namespace EasyPanel.Backend.Features.Nodes.ListNodes;

public static class ListNodesEndpoint
{
    public static void MapListNodesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/nodes", async (ListNodesHandler handler, CancellationToken cancellationToken) =>
        {
            var nodes = await handler.HandleAsync(cancellationToken);
            return Results.Ok(nodes);
        })
        .WithName("ListNodes")
        .RequireAuthorization();
    }
}
