using EasyPanel.Contracts.Enums;

namespace EasyPanel.Contracts.Control.DaemonToBackend;

/// <summary>
/// Sent once by the daemon immediately after it (re)connects, so the backend can reconcile
/// instance state after any connection gap without assuming the worst (see reconnect design in docs/architecture.md).
/// </summary>
public sealed record InstanceStatusSnapshot(
    Guid InstanceId,
    InstanceStatus CurrentStatus
);
