namespace EasyPanel.Daemon.Features.FileTransfer;

internal sealed record UploadState(FileStream Stream, string TempFilePath, string FinalFilePath);
