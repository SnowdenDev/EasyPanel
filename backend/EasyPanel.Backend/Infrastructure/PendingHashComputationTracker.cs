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
    private readonly ConcurrentDictionary<Guid, PendingHashComputation> _pending = new();

    public Task<HashComputationResult> RegisterAsync(Guid nodeId, Guid correlationId)
    {
        var completionSource = new TaskCompletionSource<HashComputationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[correlationId] = new PendingHashComputation(nodeId, completionSource);
        return completionSource.Task;
    }

    public bool Complete(Guid nodeId, HashComputationResult result)
    {
        if (!_pending.TryGetValue(result.CorrelationId, out var pending) || pending.NodeId != nodeId)
        {
            return false;
        }

        if (!_pending.TryRemove(new KeyValuePair<Guid, PendingHashComputation>(result.CorrelationId, pending)))
        {
            return false;
        }

        return pending.CompletionSource.TrySetResult(result);
    }

    public void Cancel(Guid correlationId)
    {
        _pending.TryRemove(correlationId, out _);
    }

    private sealed record PendingHashComputation(
        Guid NodeId,
        TaskCompletionSource<HashComputationResult> CompletionSource);
}
