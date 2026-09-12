using EasyPanel.Backend.Infrastructure;
using EasyPanel.Backend.Infrastructure.Entities;
using EasyPanel.Contracts.FileTransfer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Features.FileManager.UploadFile;

public sealed class UploadFileHandler(AppDbContext dbContext, FileTransferCoordinator coordinator, IHubContext<FileTransferHub> fileTransferHub)
{
    private const long RemoteNodeMaxFileSizeBytes = 10 * 1024 * 1024;
    private const int ChunkSizeBytes = 64 * 1024;
    private static readonly TimeSpan ResultTimeout = TimeSpan.FromMinutes(2);

    public async Task<UploadFileResult> HandleAsync(Guid instanceId, string relativePath, Stream requestBody, long? contentLengthBytes, CancellationToken cancellationToken)
    {
        var instance = await dbContext.Instances
            .Include(candidate => candidate.Node)
            .SingleOrDefaultAsync(candidate => candidate.Id == instanceId, cancellationToken);

        if (instance is null)
        {
            return new UploadFileResult(UploadFileOutcome.Failed, "No such instance.");
        }

        if (instance.Node is null)
        {
            return new UploadFileResult(UploadFileOutcome.NodeOffline, "Node is offline.");
        }

        // Checked before the online check deliberately — an oversized upload to a Remote
        // node is rejected on its own merits, without needing the node to be reachable at
        // all, since ConnectivityMode is a backend-owned fact independent of daemon state.
        var isRemoteAndTooLarge = instance.Node.ConnectivityMode == NodeConnectivityMode.Remote
            && (contentLengthBytes is null || contentLengthBytes > RemoteNodeMaxFileSizeBytes);

        if (isRemoteAndTooLarge)
        {
            return new UploadFileResult(UploadFileOutcome.TooLargeForRemoteNode, "File exceeds the 10 MB cap for remote nodes.");
        }

        if (!instance.Node.IsOnline)
        {
            return new UploadFileResult(UploadFileOutcome.NodeOffline, "Node is offline.");
        }

        var transferId = Guid.NewGuid();
        var reader = coordinator.RegisterTransfer(instance.NodeId, transferId);
        var nodeGroup = HubGroupNames.NodeGroup(instance.NodeId);

        try
        {
            var request = new FileTransferRequest(transferId, instance.Id, instance.WorkDirectory, relativePath, FileTransferDirection.Upload);
            await fileTransferHub.Clients.Group(nodeGroup).SendAsync("BeginFileUpload", request, cancellationToken);

            await StreamChunksAsync(transferId, requestBody, nodeGroup, cancellationToken);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(ResultTimeout);

            FileTransferEvent resultEvent;
            try
            {
                resultEvent = await reader.ReadAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                return new UploadFileResult(UploadFileOutcome.Failed, "Timed out waiting for the node to confirm the write.");
            }

            return resultEvent is FileTransferResultEvent { Succeeded: true }
                ? new UploadFileResult(UploadFileOutcome.Succeeded, null)
                : new UploadFileResult(UploadFileOutcome.Failed, (resultEvent as FileTransferResultEvent)?.FailureReason ?? "Unexpected response from node.");
        }
        finally
        {
            coordinator.CompleteAndRemove(transferId);
        }
    }

    private async Task StreamChunksAsync(Guid transferId, Stream requestBody, string nodeGroup, CancellationToken cancellationToken)
    {
        var sequenceNumber = 0L;
        var currentBuffer = await ReadFullChunkAsync(requestBody, cancellationToken);

        while (true)
        {
            var nextBuffer = await ReadFullChunkAsync(requestBody, cancellationToken);
            var isFinal = nextBuffer.Length == 0;

            var chunk = new FileChunk(transferId, sequenceNumber, currentBuffer, isFinal);
            await fileTransferHub.Clients.Group(nodeGroup).SendAsync("ReceiveFileChunk", chunk, cancellationToken);
            sequenceNumber++;

            if (isFinal)
            {
                return;
            }

            currentBuffer = nextBuffer;
        }
    }

    private static async Task<byte[]> ReadFullChunkAsync(Stream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[ChunkSizeBytes];
        var totalRead = 0;

        while (totalRead < buffer.Length)
        {
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(totalRead), cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            totalRead += bytesRead;
        }

        return totalRead == buffer.Length ? buffer : buffer[..totalRead];
    }
}
