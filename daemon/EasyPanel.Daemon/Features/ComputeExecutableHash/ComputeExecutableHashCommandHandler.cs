using EasyPanel.Contracts.Control.BackendToDaemon;
using EasyPanel.Contracts.Control.DaemonToBackend;
using EasyPanel.Daemon.Features.LaunchInstance;
using EasyPanel.Daemon.Infrastructure;
using EasyPanel.Daemon.Transport;

namespace EasyPanel.Daemon.Features.ComputeExecutableHash;

/// <summary>
/// Lets the dashboard offer "compute hash from node" while an admin is filling out the
/// create-instance form, before any Instance row exists — see
/// ComputeExecutableHashCommand's CorrelationId remark.
/// </summary>
internal sealed class ComputeExecutableHashCommandHandler(IBackendReporter reporter)
{
    public async Task HandleAsync(ComputeExecutableHashCommand command, CancellationToken cancellationToken)
    {
        if (!PathTraversalGuard.TryResolveSafePath(command.WorkDirectory, command.ExecutableRelativePath, out var fullPath) || !File.Exists(fullPath))
        {
            await reporter.ReportExecutableHashComputedAsync(command.CorrelationId, false, null, "File not found, or the path escapes the work directory.", cancellationToken);
            return;
        }

        var hashHex = await ExecutableHashVerifier.ComputeHashAsync(fullPath, cancellationToken);
        await reporter.ReportExecutableHashComputedAsync(command.CorrelationId, true, hashHex, null, cancellationToken);
    }
}
