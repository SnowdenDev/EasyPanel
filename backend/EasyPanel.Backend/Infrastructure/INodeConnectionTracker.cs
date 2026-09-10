namespace EasyPanel.Backend.Infrastructure;

/// <summary>
/// Tracks which SignalR connection is currently "the" connection for each node, so a
/// reconnecting daemon can displace a stale one instead of leaving two connections
/// claiming the same node. In-memory only — if the backend ever runs as more than one
/// process, this needs a shared backplane (e.g. Redis) instead; explicitly deferred.
/// </summary>
public interface INodeConnectionTracker
{
    /// <summary>Registers connectionId as the active one for nodeId. Returns the previous connectionId, if any.</summary>
    string? SetActiveConnection(Guid nodeId, string connectionId);

    /// <summary>Removes the tracked connection only if it still matches connectionId. Returns whether it did.</summary>
    bool RemoveIfCurrent(Guid nodeId, string connectionId);
}
