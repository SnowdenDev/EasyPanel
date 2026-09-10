namespace EasyPanel.Backend.Infrastructure;

public abstract record FileTransferEvent;

public sealed record FileTransferMetadataEvent(long TotalSizeBytes) : FileTransferEvent;

public sealed record FileTransferChunkEvent(byte[] Data, bool IsFinal) : FileTransferEvent;

public sealed record FileTransferResultEvent(bool Succeeded, string? FailureReason) : FileTransferEvent;
