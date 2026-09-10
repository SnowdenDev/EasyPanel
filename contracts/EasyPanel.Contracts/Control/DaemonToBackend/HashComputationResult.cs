namespace EasyPanel.Contracts.Control.DaemonToBackend;

public sealed record HashComputationResult(
    Guid CorrelationId,
    bool Succeeded,
    string? Sha256Hex,
    string? FailureReason
);
