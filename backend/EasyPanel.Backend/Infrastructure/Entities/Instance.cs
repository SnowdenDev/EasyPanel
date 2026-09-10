using EasyPanel.Contracts.Enums;

namespace EasyPanel.Backend.Infrastructure.Entities;

public sealed class Instance
{
    public Guid Id { get; set; }
    public Guid NodeId { get; set; }
    public Node? Node { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string WorkDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Relative to WorkDirectory — enforces "the executable lives under the work
    /// directory" at the data model level, not just as a validation rule.
    /// </summary>
    public string ExecutableRelativePath { get; set; } = string.Empty;

    public string ExpectedExecutableSha256 { get; set; } = string.Empty;
    public string? LaunchArguments { get; set; }
    public string? EnvironmentVariablesJson { get; set; }
    public InstanceStatus Status { get; set; } = InstanceStatus.Stopped;
    public bool AutoRestartEnabled { get; set; } = true;
    public int? CpuLimitPercent { get; set; }
    public int? MemoryLimitMegabytes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
