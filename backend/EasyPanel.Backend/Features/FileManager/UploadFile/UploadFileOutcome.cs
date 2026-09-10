namespace EasyPanel.Backend.Features.FileManager.UploadFile;

public enum UploadFileOutcome
{
    Succeeded,
    Failed,
    TooLargeForRemoteNode,
    NodeOffline,
}
