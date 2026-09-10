namespace EasyPanel.Contracts.Control.DaemonToBackend;

public sealed record InstanceCrashed(
    Guid InstanceId,
    int ExitCode,
    DateTimeOffset TimestampUtc,
    bool WillAutoRestart,
    DateTimeOffset? NextRestartAttemptUtc
);
