using System.Security.Claims;
using EasyPanel.Backend.Infrastructure.Security;
using Microsoft.AspNetCore.Http.Features;

namespace EasyPanel.Backend.Features.FileManager.UploadFile;

public static class UploadFileEndpoint
{
    public static void MapUploadFileEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/instances/{instanceId:guid}/files/upload", async (
            Guid instanceId,
            string path,
            HttpRequest request,
            ClaimsPrincipal user,
            IServerPermissionChecker permissionChecker,
            UploadFileHandler handler,
            CancellationToken cancellationToken) =>
        {
            var canAccessFiles = await permissionChecker.HasPermissionAsync(user, instanceId, ServerPermissionKind.AccessFileManager, cancellationToken);
            if (!canAccessFiles)
            {
                return Results.Forbid();
            }

            // Kestrel's own default request-size limit would otherwise cap a Local node's
            // supposedly-unlimited upload — our own logic below is what actually enforces
            // the 10 MB cap for Remote nodes, via ContentLength, so it's safe to lift this.
            request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>()?.MaxRequestBodySize = null;

            var result = await handler.HandleAsync(instanceId, path, request.Body, request.ContentLength, cancellationToken);

            return result.Outcome switch
            {
                UploadFileOutcome.Succeeded => Results.Ok(),
                UploadFileOutcome.NodeOffline => Results.Conflict(new { message = result.ErrorMessage }),
                UploadFileOutcome.TooLargeForRemoteNode => Results.StatusCode(StatusCodes.Status413PayloadTooLarge),
                _ => Results.BadRequest(new { message = result.ErrorMessage }),
            };
        })
        .WithName("UploadFile")
        .RequireAuthorization();
    }
}
