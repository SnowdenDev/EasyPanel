using EasyPanel.Contracts.Control.BackendToDaemon;
using EasyPanel.Contracts.Control.DaemonToBackend;
using EasyPanel.Contracts.Enums;
using EasyPanel.Contracts.Serialization;
using EasyPanel.Daemon.Features.ComputeExecutableHash;
using EasyPanel.Daemon.Features.ConsoleStreaming;
using EasyPanel.Daemon.Features.LaunchInstance;
using EasyPanel.Daemon.Features.StopInstance;
using EasyPanel.Daemon.Infrastructure;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasyPanel.Daemon.Transport;

/// <summary>
/// The daemon's one outbound connection to /hubs/daemon-control. Owns reconnect/backoff
/// beyond what SignalR's own WithAutomaticReconnect schedule covers, the heartbeat timer,
/// and is the single place that actually calls the hub — everything else in the daemon
/// talks to the backend only through IBackendReporter. See docs/architecture.md.
///
/// Takes IServiceProvider rather than LaunchInstanceCommandHandler/StopInstanceCommandHandler
/// directly and resolves them lazily in ExecuteAsync — both of those depend on
/// IBackendReporter, which this class also implements and is registered to resolve back to
/// this same singleton. Injecting them as constructor parameters would make this a real
/// dependency cycle (ControlHubConnection -> LaunchInstanceCommandHandler ->
/// IBackendReporter -> ControlHubConnection), and the DI container deadlocks on it instead
/// of throwing — the cycle runs through an opaque factory delegate, so the container's
/// call-site cycle detector can't see it coming and just blocks forever on the same
/// singleton-creation lock. Resolving lazily, after construction has already returned,
/// avoids the cycle entirely.
/// </summary>
internal sealed class ControlHubConnection(
    IOptions<NodeIdentityOptions> nodeIdentityOptions,
    LaunchedProcessRegistry registry,
    IServiceProvider serviceProvider,
    SystemStatsCollector systemStatsCollector,
    ILogger<ControlHubConnection> logger)
    : BackgroundService, IBackendReporter
{
    private static readonly TimeSpan[] AutomaticReconnectBackoff =
    [
        TimeSpan.Zero,
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30),
    ];

    private readonly NodeIdentityOptions _options = nodeIdentityOptions.Value;
    private HubConnection? _connection;

    // Resolved lazily (see the class-level remark on why) — cheap, since these are
    // singletons and this is just a dictionary lookup on an already-built instance.
    private LaunchInstanceCommandHandler LaunchInstanceCommandHandler => serviceProvider.GetRequiredService<LaunchInstanceCommandHandler>();
    private StopInstanceCommandHandler StopInstanceCommandHandler => serviceProvider.GetRequiredService<StopInstanceCommandHandler>();
    private ComputeExecutableHashCommandHandler ComputeExecutableHashCommandHandler => serviceProvider.GetRequiredService<ComputeExecutableHashCommandHandler>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl($"{_options.BackendBaseUrl}/hubs/daemon-control?nodeId={_options.NodeId}", httpOptions =>
            {
                httpOptions.AccessTokenProvider = () => Task.FromResult<string?>(_options.RawNodeToken);
            })
            .WithAutomaticReconnect(AutomaticReconnectBackoff)
            .AddJsonProtocol(jsonOptions =>
            {
                // PublishAot disables reflection-based System.Text.Json — see
                // ContractsJsonContext for why this is required even under `dotnet run`.
                jsonOptions.PayloadSerializerOptions.TypeInfoResolver = ContractsJsonContext.Default;
            })
            .Build();

        RegisterHandlers(_connection);

        _connection.Reconnected += _ => ReportCurrentInstanceStatesAsync(stoppingToken);
        _connection.Closed += error =>
        {
            logger.LogWarning(error, "Connection to backend closed — automatic reconnect exhausted, taking over manually.");
            return ReconnectLoopAsync(stoppingToken);
        };

        await StartWithRetryAsync(stoppingToken);
        await ReportCurrentInstanceStatesAsync(stoppingToken);

        using var heartbeatTimer = new PeriodicTimer(TimeSpan.FromSeconds(_options.HeartbeatIntervalSeconds));
        while (await heartbeatTimer.WaitForNextTickAsync(stoppingToken))
        {
            await SendHeartbeatAsync(stoppingToken);
        }
    }

    private void RegisterHandlers(HubConnection connection)
    {
        connection.On<LaunchInstanceCommand>("LaunchInstance", command => LaunchInstanceCommandHandler.HandleAsync(command, CancellationToken.None));
        connection.On<StopInstanceCommand>("StopInstance", command => StopInstanceCommandHandler.HandleAsync(command, CancellationToken.None));
        connection.On<Guid>("RestartInstance", RestartInstanceAsync);
        connection.On<Guid>("KillInstance", instanceId =>
            StopInstanceCommandHandler.HandleAsync(new StopInstanceCommand(instanceId, GracePeriodSeconds: 0), CancellationToken.None));
        connection.On<SendConsoleInputCommand>("SendConsoleInput", SendConsoleInputAsync);
        connection.On<ComputeExecutableHashCommand>("ComputeExecutableHash", command => ComputeExecutableHashCommandHandler.HandleAsync(command, CancellationToken.None));

        connection.On<string>("ForceDisconnect", reason =>
        {
            logger.LogWarning("Backend asked this connection to disconnect: {Reason}", reason);
            // Calling StopAsync directly from inside a message handler can hang — it waits
            // on the same receive loop that's currently running this handler. Detach it.
            _ = Task.Run(() => connection.StopAsync());
        });
    }

    private async Task RestartInstanceAsync(Guid instanceId)
    {
        if (!registry.TryGet(instanceId, out var launchedProcess))
        {
            logger.LogInformation("RestartInstance for {InstanceId} — not running, nothing to restart.", instanceId);
            return;
        }

        await StopInstanceCommandHandler.HandleAsync(new StopInstanceCommand(instanceId, GracePeriodSeconds: 10), CancellationToken.None);
        await LaunchInstanceCommandHandler.HandleAsync(launchedProcess.Command, CancellationToken.None);
    }

    private async Task SendConsoleInputAsync(SendConsoleInputCommand command)
    {
        if (registry.TryGet(command.InstanceId, out var launchedProcess))
        {
            await StdinForwarder.SendAsync(launchedProcess.Process, command.InputText, CancellationToken.None);
        }
    }

    private async Task StartWithRetryAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _connection!.StartAsync(cancellationToken);
                logger.LogInformation("Connected to backend as node {NodeId}.", _options.NodeId);
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to connect to backend, retrying in 5s.");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    private async Task ReconnectLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _connection!.State != HubConnectionState.Connected)
        {
            try
            {
                await _connection.StartAsync(cancellationToken);
                logger.LogInformation("Reconnected to backend.");
                await ReportCurrentInstanceStatesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Manual reconnect attempt failed, retrying in 30s.");
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }
    }

    private Task ReportCurrentInstanceStatesAsync(CancellationToken cancellationToken)
    {
        var snapshot = registry.RunningInstanceIds
            .Select(instanceId => new InstanceStatusSnapshot(instanceId, InstanceStatus.Running))
            .ToArray();

        return InvokeIfConnectedAsync("ReportCurrentInstanceStates", cancellationToken, snapshot);
    }

    private Task SendHeartbeatAsync(CancellationToken cancellationToken)
    {
        var stats = systemStatsCollector.GetSnapshot();
        var heartbeat = new DaemonHeartbeat(
            _options.NodeId,
            DateTimeOffset.UtcNow,
            _options.DaemonVersion,
            registry.RunningInstanceIds.ToArray(),
            stats.HostName,
            stats.LogicalProcessorCount,
            stats.TotalPhysicalMemoryMegabytes,
            stats.AvailableMemoryMegabytes,
            stats.CpuUsagePercent);
        return InvokeIfConnectedAsync("ReportHeartbeat", cancellationToken, heartbeat);
    }

    public Task ReportInstanceStatusChangedAsync(Guid instanceId, InstanceStatus newStatus, int? exitCode, CancellationToken cancellationToken) =>
        InvokeIfConnectedAsync("ReportInstanceStatusChanged", cancellationToken, new InstanceStatusChanged(instanceId, newStatus, DateTimeOffset.UtcNow, exitCode));

    public Task ReportConsoleOutputLineAsync(Guid instanceId, ConsoleStreamKind streamKind, string text, long sequenceNumber, CancellationToken cancellationToken) =>
        InvokeIfConnectedAsync("ReportConsoleOutputLine", cancellationToken, new ConsoleOutputLine(instanceId, streamKind, text, DateTimeOffset.UtcNow, sequenceNumber));

    public Task ReportInstanceCrashedAsync(Guid instanceId, int exitCode, bool willAutoRestart, DateTimeOffset? nextRestartAttemptUtc, CancellationToken cancellationToken) =>
        InvokeIfConnectedAsync("ReportInstanceCrashed", cancellationToken, new InstanceCrashed(instanceId, exitCode, DateTimeOffset.UtcNow, willAutoRestart, nextRestartAttemptUtc));

    public Task ReportExecutableHashComputedAsync(Guid correlationId, bool succeeded, string? sha256Hex, string? failureReason, CancellationToken cancellationToken) =>
        InvokeIfConnectedAsync("ReportExecutableHashComputed", cancellationToken, new HashComputationResult(correlationId, succeeded, sha256Hex, failureReason));

    private async Task InvokeIfConnectedAsync(string methodName, CancellationToken cancellationToken, object argument)
    {
        if (_connection is not { State: HubConnectionState.Connected })
        {
            logger.LogDebug("Skipped sending {Method} — not connected to the backend right now.", methodName);
            return;
        }

        try
        {
            await _connection.SendAsync(methodName, argument, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to send {Method} to the backend.", methodName);
        }
    }
}
