namespace EasyPanel.Contracts.Control.BackendToDaemon;

public sealed record SendConsoleInputCommand(
    Guid InstanceId,
    string InputText
);
