namespace EasyPanel.Contracts.Control.DaemonToBackend;

public sealed record HashComputationResult(
    Guid InstanceId,
    bool Succeeded,
    string? Sha256Hex,
    string? FailureReason
);
