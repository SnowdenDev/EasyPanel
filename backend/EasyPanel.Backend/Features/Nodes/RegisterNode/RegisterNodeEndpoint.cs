using EasyPanel.Backend.Infrastructure.Entities;
using FluentValidation;

namespace EasyPanel.Backend.Features.Nodes.RegisterNode;

public static class RegisterNodeEndpoint
{
    public static void MapRegisterNodeEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/nodes", async (
            RegisterNodeRequest request,
            IValidator<RegisterNodeRequest> validator,
            RegisterNodeHandler handler,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var response = await handler.HandleAsync(request, cancellationToken);

            return Results.Created($"/api/nodes/{response.NodeId}", response);
        })
        .WithName("RegisterNode")
        .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)));
    }
}
