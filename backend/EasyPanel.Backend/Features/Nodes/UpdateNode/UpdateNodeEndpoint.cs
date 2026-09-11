using EasyPanel.Backend.Infrastructure.Entities;
using FluentValidation;

namespace EasyPanel.Backend.Features.Nodes.UpdateNode;

public static class UpdateNodeEndpoint
{
    public static void MapUpdateNodeEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/nodes/{nodeId:guid}", async (
            Guid nodeId,
            UpdateNodeRequest request,
            IValidator<UpdateNodeRequest> validator,
            UpdateNodeHandler handler,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(nodeId, request, cancellationToken);

            return result.Succeeded
                ? Results.NoContent()
                : Results.NotFound(new { message = result.ErrorMessage });
        })
        .WithName("UpdateNode")
        .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)));
    }
}
