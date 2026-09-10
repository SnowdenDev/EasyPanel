namespace EasyPanel.Backend.Features.Instances.CreateInstance;

public sealed record CreateInstanceRequest(
    Guid NodeId,
    string DisplayName,
    string WorkDirectory,
    string ExecutableRelativePath,
    string ExpectedExecutableSha256,
    string? LaunchArguments,
    IReadOnlyDictionary<string, string>? EnvironmentVariables,
    int? CpuLimitPercent,
    int? MemoryLimitMegabytes
);
