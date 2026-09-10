namespace EasyPanel.Contracts.Control.BackendToDaemon;

public sealed record LaunchInstanceCommand(
    Guid InstanceId,
    string WorkDirectory,
    string ExecutableRelativePath,
    string ExpectedSha256,
    string? LaunchArguments,
    IReadOnlyDictionary<string, string> EnvironmentVariables,
    int? CpuLimitPercent,
    int? MemoryLimitMegabytes
);
