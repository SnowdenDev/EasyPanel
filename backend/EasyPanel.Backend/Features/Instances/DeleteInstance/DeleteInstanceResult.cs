namespace EasyPanel.Backend.Features.Instances.DeleteInstance;

public sealed record DeleteInstanceResult(bool Succeeded, string? ErrorMessage, bool NotFound = false);
