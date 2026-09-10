namespace EasyPanel.Contracts.FileTransfer;

public sealed record FileChunk(
    Guid TransferId,
    long SequenceNumber,
    byte[] Bytes,
    bool IsFinal
);
