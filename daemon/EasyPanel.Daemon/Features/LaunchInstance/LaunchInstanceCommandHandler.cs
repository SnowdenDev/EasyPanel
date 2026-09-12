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
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _launchLocks = new();

    // A crashed process is removed from LaunchedProcessRegistry immediately, before the
    // backoff delay begins — so a Stop arriving during that delay window finds nothing
    // there to cancel, and without this, the scheduled restart fires anyway once the delay
    // elapses, relaunching an instance the admin just explicitly stopped. This dictionary
    // is what StopInstanceCommandHandler.CancelPendingRestart actually cancels.
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _pendingRestarts = new();

    /// <summary>The entry point for a real "start this instance" request from the backend.</summary>
    public Task HandleAsync(LaunchInstanceCommand command, CancellationToken cancellationToken) =>
        LaunchSerializedAsync(command, resetRestartAttempts: true, cancellationToken);

    private async Task LaunchSerializedAsync(
        LaunchInstanceCommand command,
        bool resetRestartAttempts,
        CancellationToken cancellationToken)
    {
        var launchLock = _launchLocks.GetOrAdd(command.InstanceId, _ => new SemaphoreSlim(1, 1));
        await launchLock.WaitAsync(cancellationToken);

        try
        {
            if (resetRestartAttempts)
            {
                _restartAttempts[command.InstanceId] = 0;
            }

            if (registry.TryGet(command.InstanceId, out _))
            {
                logger.LogInformation("LaunchInstance for {InstanceId} ignored because it is already running.", command.InstanceId);
                await reporter.ReportInstanceStatusChangedAsync(command.InstanceId, InstanceStatus.Running, null, cancellationToken);
                return;
            }

            await LaunchAsync(command, cancellationToken);
        }
        finally
        {
            launchLock.Release();
        }
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

        var process = new Process { StartInfo = startInfo };
        var outputPump = new ConsoleOutputPump(command.InstanceId, reporter);
        outputPump.AttachTo(process);
        ManagedJobObject? jobObject = null;
        var processStarted = false;
        var processRegistered = false;

        try
        {
            // Create the Job Object before starting the process and assign it immediately
            // afterward. Process.Start cannot create suspended processes, so this keeps the
            // unavoidable pre-assignment window as short as the managed API permits.
            var memoryLimitBytes = command.MemoryLimitMegabytes is { } megabytes ? megabytes * 1024L * 1024L : (long?)null;
            jobObject = ManagedJobObject.Create(command.CpuLimitPercent, memoryLimitBytes);

            if (!process.Start())
            {
                logger.LogError("Process.Start returned false for instance {InstanceId}.", command.InstanceId);
                await reporter.ReportInstanceStatusChangedAsync(command.InstanceId, InstanceStatus.Crashed, null, cancellationToken);
                return;
            }

            processStarted = true;
            jobObject.AssignProcess(process);

            var launchedProcess = new LaunchedProcess(process, jobObject, command);
            if (!registry.TryAdd(command.InstanceId, launchedProcess))
            {
                logger.LogWarning("A concurrent launch already registered instance {InstanceId}; terminating the duplicate process.", command.InstanceId);
                jobObject.TerminateAll();
                return;
            }

            processRegistered = true;

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Publish Running before enabling exit events. If a short-lived executable
            // already exited, enabling events immediately afterward raises Exited and the
            // later Crashed update remains the final state seen by the backend.
            await reporter.ReportInstanceStatusChangedAsync(command.InstanceId, InstanceStatus.Running, null, cancellationToken);

            process.Exited += (_, _) => OnProcessExited(command.InstanceId);
            process.EnableRaisingEvents = true;

            jobObject = null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to launch instance {InstanceId} under a Job Object.", command.InstanceId);

            if (processRegistered)
            {
                registry.TryRemove(command.InstanceId, out _);
            }

            if (processStarted && !process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            await reporter.ReportInstanceStatusChangedAsync(command.InstanceId, InstanceStatus.Crashed, null, CancellationToken.None);
        }
        finally
        {
            jobObject?.Dispose();
            if (jobObject is not null)
            {
                process.Dispose();
            }
        }
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
        launchedProcess.Process.Dispose();
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

            await LaunchSerializedAsync(launchedProcess.Command, resetRestartAttempts: false, CancellationToken.None);
        });
    }
}
