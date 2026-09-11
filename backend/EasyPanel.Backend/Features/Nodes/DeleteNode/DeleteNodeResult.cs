namespace EasyPanel.Backend.Features.Nodes.DeleteNode;

public sealed record DeleteNodeResult(bool Succeeded, string? ErrorMessage, bool NotFound = false);
