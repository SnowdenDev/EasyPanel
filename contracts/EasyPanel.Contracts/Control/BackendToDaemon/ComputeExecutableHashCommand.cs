namespace EasyPanel.Contracts.Control.BackendToDaemon;

public sealed record ComputeExecutableHashCommand(
    Guid InstanceId,
    string WorkDirectory,
    string ExecutableRelativePath
);
