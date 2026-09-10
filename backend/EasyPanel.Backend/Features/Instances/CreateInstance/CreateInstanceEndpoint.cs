using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EasyPanel.Backend.Infrastructure.Entities;
using FluentValidation;

namespace EasyPanel.Backend.Features.Instances.CreateInstance;

public static class CreateInstanceEndpoint
{
    public static void MapCreateInstanceEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/instances", async (
            CreateInstanceRequest request,
            ClaimsPrincipal user,
            IValidator<CreateInstanceRequest> validator,
            CreateInstanceHandler handler,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var actorUserId = Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            var result = await handler.HandleAsync(request, actorUserId, cancellationToken);

            if (result.NodeNotFoundError is not null)
            {
                return Results.NotFound(new { message = result.NodeNotFoundError });
            }

            return Results.Created($"/api/instances/{result.Response!.InstanceId}", result.Response);
        })
        .WithName("CreateInstance")
        .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)));
    }
}
