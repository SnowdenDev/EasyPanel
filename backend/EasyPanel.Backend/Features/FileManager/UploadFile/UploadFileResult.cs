namespace EasyPanel.Backend.Features.FileManager.UploadFile;

public sealed record UploadFileResult(UploadFileOutcome Outcome, string? ErrorMessage);
