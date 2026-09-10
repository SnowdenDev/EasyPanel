namespace EasyPanel.Contracts.Control.BackendToDaemon;

public sealed record StopInstanceCommand(
    Guid InstanceId,
    int GracePeriodSeconds
);
