using EasyPanel.Backend.Infrastructure;
using EasyPanel.Backend.Infrastructure.Security;

namespace EasyPanel.Backend.Features.FileManager.DownloadFile;

public static class DownloadFileEndpoint
{
    public static void MapDownloadFileEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/instances/{instanceId:guid}/files/download", async (
            Guid instanceId,
            string path,
            System.Security.Claims.ClaimsPrincipal user,
            IServerPermissionChecker permissionChecker,
            DownloadFileHandler handler,
            FileTransferCoordinator coordinator,
            CancellationToken cancellationToken) =>
        {
            var canAccessFiles = await permissionChecker.HasPermissionAsync(user, instanceId, ServerPermissionKind.AccessFileManager, cancellationToken);
            if (!canAccessFiles)
            {
                return Results.Forbid();
            }

            var start = await handler.HandleAsync(instanceId, path, cancellationToken);

            switch (start.Outcome)
            {
                case DownloadFileOutcome.NodeOffline:
                    return Results.Conflict(new { message = start.ErrorMessage });
                case DownloadFileOutcome.TooLargeForRemoteNode:
                    return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
                case DownloadFileOutcome.NotFound:
                    return Results.NotFound(new { message = start.ErrorMessage });
            }

            var transferId = start.TransferId;
            var reader = start.Reader!;

            return Results.Stream(
                async outputStream =>
                {
                    try
                    {
                        while (await reader.WaitToReadAsync(cancellationToken))
                        {
                            while (reader.TryRead(out var @event))
                            {
                                switch (@event)
                                {
                                    case FileTransferChunkEvent chunk:
                                        await outputStream.WriteAsync(chunk.Data, cancellationToken);
                                        if (chunk.IsFinal)
                                        {
                                            return;
                                        }
                                        break;

                                    case FileTransferResultEvent { Succeeded: false } failure:
                                        throw new IOException(failure.FailureReason ?? "Transfer failed.");

                                    case FileTransferResultEvent:
                                        return;
                                }
                            }
                        }
                    }
                    finally
                    {
                        coordinator.CompleteAndRemove(transferId);
                    }
                },
                "application/octet-stream",
                start.FileName);
        })
        .WithName("DownloadFile")
        .RequireAuthorization();
    }
}
