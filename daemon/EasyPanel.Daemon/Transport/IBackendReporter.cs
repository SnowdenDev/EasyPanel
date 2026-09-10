using EasyPanel.Contracts.Enums;

namespace EasyPanel.Daemon.Transport;

/// <summary>
/// What LaunchInstance/ConsoleStreaming/RestartPolicy need to tell the backend, kept as an
/// abstraction over ControlHubConnection so those features don't depend on SignalR directly.
/// </summary>
internal interface IBackendReporter
{
    Task ReportInstanceStatusChangedAsync(Guid instanceId, InstanceStatus newStatus, int? exitCode, CancellationToken cancellationToken);

    Task ReportConsoleOutputLineAsync(Guid instanceId, ConsoleStreamKind streamKind, string text, long sequenceNumber, CancellationToken cancellationToken);

    Task ReportInstanceCrashedAsync(Guid instanceId, int exitCode, bool willAutoRestart, DateTimeOffset? nextRestartAttemptUtc, CancellationToken cancellationToken);
}
