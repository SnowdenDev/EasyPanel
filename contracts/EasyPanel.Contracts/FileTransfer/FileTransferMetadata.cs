namespace EasyPanel.Contracts.FileTransfer;

/// <summary>
/// Sent once by the daemon immediately after opening a file for download, before any
/// chunk — lets the backend enforce the Local/Remote size cap by checking the real file
/// size before it starts relaying bytes to the browser, not partway through.
/// </summary>
public sealed record FileTransferMetadata(Guid TransferId, long TotalSizeBytes);
