namespace EasyPanel.Backend.Features.FileManager.DownloadFile;

public enum DownloadFileOutcome
{
    Ready,
    NotFound,
    TooLargeForRemoteNode,
    NodeOffline,
}
