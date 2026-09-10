using EasyPanel.Contracts.Control.BackendToDaemon;
using EasyPanel.Contracts.Enums;
using EasyPanel.Daemon.Features.LaunchInstance;
using EasyPanel.Daemon.Transport;
using Microsoft.Extensions.Logging;

namespace EasyPanel.Daemon.Features.StopInstance;

internal sealed class StopInstanceCommandHandler(
    LaunchedProcessRegistry registry,
    LaunchInstanceCommandHandler launchInstanceCommandHandler,
    IBackendReporter reporter,
    ILogger<StopInstanceCommandHandler> logger)
{
    public async Task HandleAsync(StopInstanceCommand command, CancellationToken cancellationToken)
    {
        // A crashed instance sits in a backoff delay, not in the registry, waiting to
        // auto-restart — cancel that unconditionally, whether or not anything is actually
        // running right now, or a Stop that lands mid-backoff gets silently overridden by
        // the pending restart once its delay elapses. See LaunchInstanceCommandHandler.
        launchInstanceCommandHandler.CancelPendingRestart(command.InstanceId);

        // Removing first (before the process has actually exited) is what makes
        // LaunchInstanceCommandHandler's Process.Exited handler treat this as an expected
        // stop rather than a crash — see docs/architecture.md.
        if (!registry.TryRemove(command.InstanceId, out var launchedProcess))
        {
            logger.LogInformation("StopInstance for {InstanceId} — nothing running under that id, nothing to do.", command.InstanceId);
            return;
        }

        try
        {
            // There's no generic cross-game "graceful shutdown" signal on Windows short of
            // the process choosing to listen for one on its own stdin — this just gives it
            // a grace window to exit on its own before the Job Object hard-kills it.
            var exitedGracefully = await WaitForExitAsync(launchedProcess.Process, TimeSpan.FromSeconds(command.GracePeriodSeconds), cancellationToken);

            if (!exitedGracefully)
            {
                launchedProcess.JobObject.TerminateAll();
            }
        }
        finally
        {
            launchedProcess.JobObject.Dispose();
        }

        var exitCode = launchedProcess.Process.HasExited ? launchedProcess.Process.ExitCode : (int?)null;
        await reporter.ReportInstanceStatusChangedAsync(command.InstanceId, InstanceStatus.Stopped, exitCode, cancellationToken);
    }

    private static async Task<bool> WaitForExitAsync(System.Diagnostics.Process process, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }
}
