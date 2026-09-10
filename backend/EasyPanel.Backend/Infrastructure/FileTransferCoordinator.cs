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
    private readonly ConcurrentDictionary<Guid, Channel<FileTransferEvent>> _channelsByTransferId = new();

    public ChannelReader<FileTransferEvent> RegisterTransfer(Guid transferId)
    {
        var channel = Channel.CreateUnbounded<FileTransferEvent>();
        _channelsByTransferId[transferId] = channel;
        return channel.Reader;
    }

    public void CompleteAndRemove(Guid transferId)
    {
        if (_channelsByTransferId.TryRemove(transferId, out var channel))
        {
            channel.Writer.TryComplete();
        }
    }

    public void PushMetadata(Guid transferId, long totalSizeBytes) =>
        Push(transferId, new FileTransferMetadataEvent(totalSizeBytes));

    public void PushChunk(Guid transferId, byte[] data, bool isFinal) =>
        Push(transferId, new FileTransferChunkEvent(data, isFinal));

    public void PushResult(Guid transferId, bool succeeded, string? failureReason) =>
        Push(transferId, new FileTransferResultEvent(succeeded, failureReason));

    private void Push(Guid transferId, FileTransferEvent @event)
    {
        if (_channelsByTransferId.TryGetValue(transferId, out var channel))
        {
            channel.Writer.TryWrite(@event);
        }
    }
}
