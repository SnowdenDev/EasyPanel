namespace EasyPanel.Contracts.FileTransfer;

public sealed record FileTransferRequest(
    Guid TransferId,
    Guid InstanceId,
    string RelativePath,
    FileTransferDirection Direction
);
