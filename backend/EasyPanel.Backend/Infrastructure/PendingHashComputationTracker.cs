using System.Collections.Concurrent;
using EasyPanel.Contracts.Control.DaemonToBackend;

namespace EasyPanel.Backend.Infrastructure;

/// <summary>
/// Bridges DaemonControlHub.ReportExecutableHashComputed (a fire-and-forget SignalR call
/// from the daemon) back to the REST request that's awaiting it, correlated by
/// CorrelationId — same pattern as FileTransferCoordinator, just single-shot instead of a
/// stream of events.
/// </summary>
public sealed class PendingHashComputationTracker
{
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<HashComputationResult>> _pending = new();

    public Task<HashComputationResult> RegisterAsync(Guid correlationId)
    {
        var completionSource = new TaskCompletionSource<HashComputationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[correlationId] = completionSource;
        return completionSource.Task;
    }

    public void Complete(HashComputationResult result)
    {
        if (_pending.TryRemove(result.CorrelationId, out var completionSource))
        {
            completionSource.TrySetResult(result);
        }
    }

    public void Cancel(Guid correlationId)
    {
        _pending.TryRemove(correlationId, out _);
    }
}
