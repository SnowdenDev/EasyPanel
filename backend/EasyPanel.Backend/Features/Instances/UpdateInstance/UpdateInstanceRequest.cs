namespace EasyPanel.Backend.Features.Instances.UpdateInstance;

// Deliberately excludes NodeId — moving an existing instance to a different node is a
// bigger operation than an in-place edit (the daemon on the old node has no idea the
// instance moved), and isn't something this endpoint tries to support.
public sealed record UpdateInstanceRequest(
    string DisplayName,
    string WorkDirectory,
    string ExecutableRelativePath,
    string ExpectedExecutableSha256,
    string? LaunchArguments,
    IReadOnlyDictionary<string, string>? EnvironmentVariables,
    int? CpuLimitPercent,
    int? MemoryLimitMegabytes
);
