using System.Collections.Concurrent;
using System.Threading.Channels;

namespace EasyPanel.Backend.Infrastructure;

/// <summary>
/// Bridges a daemon's FileTransferHub calls (which arrive as fire-and-forget SignalR
/// invocations, correlated only by TransferId) to the open HTTP request that's waiting
/// on them — one Channel per in-flight transfer. See docs/architecture.md's file
/// transfer section for why this relay exists at all (the daemon has no inbound port).
/// </summary>
public sealed class FileTransferCoordinator
{
    private const int BufferedEventCapacity = 16;
    private readonly ConcurrentDictionary<Guid, TransferState> _transfersById = new();

    public ChannelReader<FileTransferEvent> RegisterTransfer(Guid nodeId, Guid transferId)
    {
        var channel = Channel.CreateBounded<FileTransferEvent>(new BoundedChannelOptions(BufferedEventCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait,
        });

        _transfersById[transferId] = new TransferState(nodeId, channel);
        return channel.Reader;
    }

    public void CompleteAndRemove(Guid transferId)
    {
        if (_transfersById.TryRemove(transferId, out var transfer))
        {
            transfer.Channel.Writer.TryComplete();
        }
    }

    public ValueTask<bool> PushMetadataAsync(Guid nodeId, Guid transferId, long totalSizeBytes, CancellationToken cancellationToken) =>
        PushAsync(nodeId, transferId, new FileTransferMetadataEvent(totalSizeBytes), cancellationToken);

    public ValueTask<bool> PushChunkAsync(Guid nodeId, Guid transferId, byte[] data, bool isFinal, CancellationToken cancellationToken) =>
        PushAsync(nodeId, transferId, new FileTransferChunkEvent(data, isFinal), cancellationToken);

    public ValueTask<bool> PushResultAsync(Guid nodeId, Guid transferId, bool succeeded, string? failureReason, CancellationToken cancellationToken) =>
        PushAsync(nodeId, transferId, new FileTransferResultEvent(succeeded, failureReason), cancellationToken);

    private async ValueTask<bool> PushAsync(Guid nodeId, Guid transferId, FileTransferEvent @event, CancellationToken cancellationToken)
    {
        if (!_transfersById.TryGetValue(transferId, out var transfer) || transfer.NodeId != nodeId)
        {
            return false;
        }

        try
        {
            await transfer.Channel.Writer.WriteAsync(@event, cancellationToken);
            return true;
        }
        catch (ChannelClosedException)
        {
            return false;
        }
    }

    private sealed record TransferState(Guid NodeId, Channel<FileTransferEvent> Channel);
}
