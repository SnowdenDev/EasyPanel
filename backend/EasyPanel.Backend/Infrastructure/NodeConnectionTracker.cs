using System.Collections.Concurrent;

namespace EasyPanel.Backend.Infrastructure;

public sealed class NodeConnectionTracker : INodeConnectionTracker
{
    private readonly ConcurrentDictionary<Guid, string> _activeConnectionIdByNodeId = new();

    public string? SetActiveConnection(Guid nodeId, string connectionId)
    {
        string? previousConnectionId = null;

        _activeConnectionIdByNodeId.AddOrUpdate(
            nodeId,
            connectionId,
            (_, existingConnectionId) =>
            {
                previousConnectionId = existingConnectionId;
                return connectionId;
            });

        return previousConnectionId;
    }

    public bool RemoveIfCurrent(Guid nodeId, string connectionId)
    {
        var isStillCurrent = _activeConnectionIdByNodeId.TryGetValue(nodeId, out var currentConnectionId)
            && currentConnectionId == connectionId;

        if (isStillCurrent)
        {
            _activeConnectionIdByNodeId.TryRemove(nodeId, out _);
        }

        return isStillCurrent;
    }
}
