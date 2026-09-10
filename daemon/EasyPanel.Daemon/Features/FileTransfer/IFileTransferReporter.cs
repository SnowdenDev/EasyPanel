namespace EasyPanel.Daemon.Features.FileTransfer;

internal interface IFileTransferReporter
{
    Task ReportMetadataAsync(Guid transferId, long totalSizeBytes);

    Task SendChunkAsync(Guid transferId, long sequenceNumber, byte[] data, bool isFinal);

    Task ReportResultAsync(Guid transferId, bool succeeded, string? failureReason);
}
