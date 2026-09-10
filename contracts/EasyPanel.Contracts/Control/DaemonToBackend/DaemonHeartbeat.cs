namespace EasyPanel.Contracts.Control.DaemonToBackend;

public sealed record DaemonHeartbeat(
    Guid NodeId,
    DateTimeOffset TimestampUtc,
    string DaemonVersion,
    IReadOnlyList<Guid> RunningInstanceIds
);
