using EasyPanel.Contracts.Enums;

namespace EasyPanel.Contracts.Control.DaemonToBackend;

public sealed record InstanceStatusChanged(
    Guid InstanceId,
    InstanceStatus NewStatus,
    DateTimeOffset TimestampUtc,
    int? ExitCode
);
