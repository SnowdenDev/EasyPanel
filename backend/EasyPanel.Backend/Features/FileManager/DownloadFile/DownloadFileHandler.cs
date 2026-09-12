using System.Threading.Channels;
using EasyPanel.Backend.Infrastructure;
using EasyPanel.Backend.Infrastructure.Entities;
using EasyPanel.Contracts.FileTransfer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Features.FileManager.DownloadFile;

public sealed record DownloadFileStart(
    DownloadFileOutcome Outcome,
    string? ErrorMessage,
    Guid TransferId,
    string? FileName,
    ChannelReader<FileTransferEvent>? Reader
);

public sealed class DownloadFileHandler(AppDbContext dbContext, FileTransferCoordinator coordinator, IHubContext<FileTransferHub> fileTransferHub)
{
    private const long RemoteNodeMaxFileSizeBytes = 10 * 1024 * 1024;
    private static readonly TimeSpan FirstEventTimeout = TimeSpan.FromSeconds(15);

    public async Task<DownloadFileStart> HandleAsync(Guid instanceId, string relativePath, CancellationToken cancellationToken)
    {
        var instance = await dbContext.Instances
            .Include(candidate => candidate.Node)
            .SingleOrDefaultAsync(candidate => candidate.Id == instanceId, cancellationToken);

        if (instance is null)
        {
            return new DownloadFileStart(DownloadFileOutcome.NotFound, "No such instance.", Guid.Empty, null, null);
        }

        if (instance.Node is null || !instance.Node.IsOnline)
        {
            return new DownloadFileStart(DownloadFileOutcome.NodeOffline, "Node is offline.", Guid.Empty, null, null);
        }

        var transferId = Guid.NewGuid();
        var reader = coordinator.RegisterTransfer(instance.NodeId, transferId);

        var request = new FileTransferRequest(transferId, instance.Id, instance.WorkDirectory, relativePath, FileTransferDirection.Download);
        await fileTransferHub.Clients.Group(HubGroupNames.NodeGroup(instance.NodeId)).SendAsync("BeginFileDownload", request, cancellationToken);

        FileTransferEvent? firstEvent;
        using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            timeoutCts.CancelAfter(FirstEventTimeout);
            try
            {
                firstEvent = await reader.ReadAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                coordinator.CompleteAndRemove(transferId);
                return new DownloadFileStart(DownloadFileOutcome.NotFound, "Timed out waiting for the node to respond.", transferId, null, null);
            }
        }

        if (firstEvent is FileTransferResultEvent { Succeeded: false } failure)
        {
            coordinator.CompleteAndRemove(transferId);
            return new DownloadFileStart(DownloadFileOutcome.NotFound, failure.FailureReason, transferId, null, null);
        }

        if (firstEvent is not FileTransferMetadataEvent metadata)
        {
            coordinator.CompleteAndRemove(transferId);
            return new DownloadFileStart(DownloadFileOutcome.NotFound, "Unexpected response from node.", transferId, null, null);
        }

        var isRemoteAndTooLarge = instance.Node.ConnectivityMode == NodeConnectivityMode.Remote && metadata.TotalSizeBytes > RemoteNodeMaxFileSizeBytes;
        if (isRemoteAndTooLarge)
        {
            await fileTransferHub.Clients.Group(HubGroupNames.NodeGroup(instance.NodeId)).SendAsync("CancelFileTransfer", transferId, cancellationToken);
            coordinator.CompleteAndRemove(transferId);
            return new DownloadFileStart(DownloadFileOutcome.TooLargeForRemoteNode, "File exceeds the 10 MB cap for remote nodes.", transferId, null, null);
        }

        return new DownloadFileStart(DownloadFileOutcome.Ready, null, transferId, Path.GetFileName(relativePath), reader);
    }
}
