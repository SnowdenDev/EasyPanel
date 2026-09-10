namespace EasyPanel.Contracts.FileTransfer;

public sealed record FileTransferResult(
    Guid TransferId,
    bool Succeeded,
    string? FailureReason
);
