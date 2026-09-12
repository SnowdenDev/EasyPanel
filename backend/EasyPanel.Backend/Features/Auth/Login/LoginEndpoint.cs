using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;

namespace EasyPanel.Backend.Features.Auth.Login;

public static class LoginEndpoint
{
    public static void MapLoginEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (
            LoginRequest request,
            IValidator<LoginRequest> validator,
            LoginHandler handler,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var response = await handler.HandleAsync(request, cancellationToken);

            return response is null
                ? Results.Unauthorized()
                : Results.Ok(response);
        })
        .WithName("Login")
        .RequireRateLimiting("login")
        .AllowAnonymous();
    }
}
