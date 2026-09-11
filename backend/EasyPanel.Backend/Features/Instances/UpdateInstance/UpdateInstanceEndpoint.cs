using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EasyPanel.Backend.Infrastructure.Security;
using FluentValidation;

namespace EasyPanel.Backend.Features.Instances.UpdateInstance;

public static class UpdateInstanceEndpoint
{
    public static void MapUpdateInstanceEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/instances/{instanceId:guid}", async (
            Guid instanceId,
            UpdateInstanceRequest request,
            ClaimsPrincipal user,
            IServerPermissionChecker permissionChecker,
            IValidator<UpdateInstanceRequest> validator,
            UpdateInstanceHandler handler,
            CancellationToken cancellationToken) =>
        {
            var canEditSettings = await permissionChecker.HasPermissionAsync(user, instanceId, ServerPermissionKind.EditSettings, cancellationToken);
            if (!canEditSettings)
            {
                return Results.Forbid();
            }

            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var actorUserId = Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            var result = await handler.HandleAsync(instanceId, request, actorUserId, cancellationToken);

            return result.Succeeded
                ? Results.NoContent()
                : Results.NotFound(new { message = result.ErrorMessage });
        })
        .WithName("UpdateInstance")
        .RequireAuthorization();
    }
}
