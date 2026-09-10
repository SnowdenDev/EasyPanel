namespace EasyPanel.Backend.Features.Nodes.ComputeExecutableHash;

public sealed record ComputeExecutableHashResult(bool Succeeded, string? Sha256Hex, string? ErrorMessage);
