namespace EasyPanel.Contracts.Control.BackendToDaemon;

/// <summary>
/// CorrelationId is a request id the backend makes up for this round trip — not
/// necessarily a real Instance — since this is most useful before an instance exists yet
/// (the admin picking "compute hash from node" while filling out the create-instance form).
/// </summary>
public sealed record ComputeExecutableHashCommand(
    Guid CorrelationId,
    string WorkDirectory,
    string ExecutableRelativePath
);
