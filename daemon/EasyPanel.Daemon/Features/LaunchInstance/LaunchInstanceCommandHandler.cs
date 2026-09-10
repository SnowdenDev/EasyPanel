using System.Collections.Concurrent;
using System.Diagnostics;
using EasyPanel.Contracts.Control.BackendToDaemon;
using EasyPanel.Contracts.Enums;
using EasyPanel.Daemon.Features.ConsoleStreaming;
using EasyPanel.Daemon.Features.RestartPolicy;
using EasyPanel.Daemon.Infrastructure;
using EasyPanel.Daemon.JobObjects;
using EasyPanel.Daemon.Transport;
using Microsoft.Extensions.Logging;

namespace EasyPanel.Daemon.Features.LaunchInstance;

internal sealed class LaunchInstanceCommandHandler(
    LaunchedProcessRegistry registry,
    IBackendReporter reporter,
    ILogger<LaunchInstanceCommandHandler> logger)
{
    private readonly ConcurrentDictionary<Guid, int> _restartAttempts = new();

    // A crashed process is removed from LaunchedProcessRegistry immediately, before the
    // backoff delay begins — so a Stop arriving during that delay window finds nothing
    // there to cancel, and without this, the scheduled restart fires anyway once the delay
    // elapses, relaunching an instance the admin just explicitly stopped. This dictionary
    // is what StopInstanceCommandHandler.CancelPendingRestart actually cancels.
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _pendingRestarts = new();

    /// <summary>The entry point for a real "start this instance" request from the backend.</summary>
    public Task HandleAsync(LaunchInstanceCommand command, CancellationToken cancellationToken)
    {
        _restartAttempts[command.InstanceId] = 0;
        return LaunchAsync(command, cancellationToken);
    }

    /// <summary>Called by StopInstanceCommandHandler — cancels a restart that's currently waiting out its backoff delay, if any.</summary>
    public void CancelPendingRestart(Guid instanceId)
    {
        if (_pendingRestarts.TryRemove(instanceId, out var cancellationSource))
        {
            cancellationSource.Cancel();
        }
    }

    private async Task LaunchAsync(LaunchInstanceCommand command, CancellationToken cancellationToken)
    {
        if (!PathTraversalGuard.TryResolveSafePath(command.WorkDirectory, command.ExecutableRelativePath, out var executableFullPath))
        {
            logger.LogWarning("Refusing to launch instance {InstanceId} — executable path escapes the work directory.", command.InstanceId);
            await reporter.ReportInstanceStatusChangedAsync(command.InstanceId, InstanceStatus.HashMismatchRefused, null, cancellationToken);
            return;
        }

        if (!File.Exists(executableFullPath))
        {
            logger.LogWarning("Refusing to launch instance {InstanceId} — executable not found at {Path}.", command.InstanceId, executableFullPath);
            await reporter.ReportInstanceStatusChangedAsync(command.InstanceId, InstanceStatus.HashMismatchRefused, null, cancellationToken);
            return;
        }

        var hashMatches = await ExecutableHashVerifier.MatchesExpectedHashAsync(executableFullPath, command.ExpectedSha256, cancellationToken);
        if (!hashMatches)
        {
            logger.LogWarning("Refusing to launch instance {InstanceId} — SHA256 does not match the expected hash.", command.InstanceId);
            await reporter.ReportInstanceStatusChangedAsync(command.InstanceId, InstanceStatus.HashMismatchRefused, null, cancellationToken);
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executableFullPath,
            WorkingDirectory = command.WorkDirectory,
            Arguments = command.LaunchArguments ?? string.Empty,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
        };

        foreach (var (key, value) in command.EnvironmentVariables)
        {
            startInfo.Environment[key] = value;
        }

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        var outputPump = new ConsoleOutputPump(command.InstanceId, reporter);
        outputPump.AttachTo(process);
        process.Exited += (_, _) => OnProcessExited(command.InstanceId);

        if (!process.Start())
        {
            logger.LogError("Process.Start returned false for instance {InstanceId}.", command.InstanceId);
            await reporter.ReportInstanceStatusChangedAsync(command.InstanceId, InstanceStatus.Crashed, null, cancellationToken);
            return;
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var memoryLimitBytes = command.MemoryLimitMegabytes is { } megabytes ? megabytes * 1024L * 1024L : (long?)null;
        var jobObject = ManagedJobObject.Create(command.CpuLimitPercent, memoryLimitBytes);

        try
        {
            jobObject.AssignProcess(process);
        }
        catch
        {
            // The process is already running and would otherwise leak unmanaged — better to
            // kill it outright than leave an unsupervised game server process behind.
            process.Kill(entireProcessTree: true);
            jobObject.Dispose();
            throw;
        }

        registry.TryAdd(command.InstanceId, new LaunchedProcess(process, jobObject, command));

        await reporter.ReportInstanceStatusChangedAsync(command.InstanceId, InstanceStatus.Running, null, cancellationToken);
    }

    private void OnProcessExited(Guid instanceId)
    {
        // If this fails, StopInstanceCommandHandler already removed the entry as part of a
        // deliberate stop — this exit is expected, not a crash, and there's nothing to do.
        if (!registry.TryRemove(instanceId, out var launchedProcess))
        {
            return;
        }

        launchedProcess.JobObject.Dispose();

        var exitCode = launchedProcess.Process.ExitCode;
        var attemptNumber = _restartAttempts.AddOrUpdate(instanceId, 1, (_, previous) => previous + 1);

        if (!launchedProcess.Command.AutoRestartEnabled)
        {
            _ = reporter.ReportInstanceCrashedAsync(instanceId, exitCode, willAutoRestart: false, nextRestartAttemptUtc: null, CancellationToken.None);
            return;
        }

        var delay = CrashBackoffCalculator.GetDelayForAttempt(attemptNumber - 1);
        var nextRestartAttemptUtc = DateTimeOffset.UtcNow.Add(delay);

        _ = reporter.ReportInstanceCrashedAsync(instanceId, exitCode, willAutoRestart: true, nextRestartAttemptUtc, CancellationToken.None);

        var cancellationSource = new CancellationTokenSource();
        _pendingRestarts[instanceId] = cancellationSource;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay, cancellationSource.Token);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Scheduled restart for {InstanceId} was cancelled — it was stopped before the backoff delay elapsed.", instanceId);
                return;
            }
            finally
            {
                _pendingRestarts.TryRemove(instanceId, out _);
            }

            await LaunchAsync(launchedProcess.Command, CancellationToken.None);
        });
    }
}
