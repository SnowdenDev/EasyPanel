namespace EasyPanel.Contracts.FileTransfer;

public sealed record FileTransferRequest(
    Guid TransferId,
    Guid InstanceId,
    string WorkDirectory,
    string RelativePath,
    FileTransferDirection Direction
);
