using EasyPanel.Backend.Infrastructure.Entities;
using FluentValidation;

namespace EasyPanel.Backend.Features.Users.CreateUser;

public static class CreateUserEndpoint
{
    public static void MapCreateUserEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users", async (
            CreateUserRequest request,
            IValidator<CreateUserRequest> validator,
            CreateUserHandler handler,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(request, cancellationToken);

            return result.EmailAlreadyInUseError is not null
                ? Results.Conflict(new { message = result.EmailAlreadyInUseError })
                : Results.Created($"/api/users/{result.UserId}", new { userId = result.UserId });
        })
        .WithName("CreateUser")
        .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)));
    }
}
