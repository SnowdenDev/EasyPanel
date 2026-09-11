using EasyPanel.Backend.Infrastructure.Entities;

namespace EasyPanel.Backend.Features.Nodes.ListNodes;

public sealed record NodeSummary(
    Guid Id,
    string DisplayName,
    NodeConnectivityMode ConnectivityMode,
    bool IsOnline,
    DateTimeOffset? LastHeartbeatAtUtc,
    string? DaemonVersion,
    string? HostName,
    int? LogicalProcessorCount,
    long? TotalPhysicalMemoryMegabytes,
    long? AvailableMemoryMegabytes,
    double? CpuUsagePercent
);
