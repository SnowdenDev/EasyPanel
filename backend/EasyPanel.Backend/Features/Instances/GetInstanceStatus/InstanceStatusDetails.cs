using EasyPanel.Contracts.Enums;

namespace EasyPanel.Backend.Features.Instances.GetInstanceStatus;

// Started as a narrow status-only shape for the console page; grew the editable fields
// too once the Dashboard needed an instance-detail/edit view, rather than standing up a
// second near-duplicate per-instance GET endpoint. Existing callers that only read the
// original five fields are unaffected — this is purely additive.
public sealed record InstanceStatusDetails(
    Guid InstanceId,
    string DisplayName,
    InstanceStatus Status,
    Guid NodeId,
    bool IsNodeOnline,
    DateTimeOffset UpdatedAtUtc,
    string WorkDirectory,
    string ExecutableRelativePath,
    string ExpectedExecutableSha256,
    string? LaunchArguments,
    IReadOnlyDictionary<string, string>? EnvironmentVariables,
    int? CpuLimitPercent,
    int? MemoryLimitMegabytes
);
