using System.Collections.Concurrent;

namespace EasyPanel.Daemon.Features.LaunchInstance;

/// <summary>
/// Tracks every instance this daemon currently has running. Also the source of truth for
/// RunningInstanceIds in the heartbeat and for ReportCurrentInstanceStates on reconnect.
/// </summary>
internal sealed class LaunchedProcessRegistry
{
    private readonly ConcurrentDictionary<Guid, LaunchedProcess> _launchedProcesses = new();

    public bool TryAdd(Guid instanceId, LaunchedProcess launchedProcess) => _launchedProcesses.TryAdd(instanceId, launchedProcess);

    public bool TryGet(Guid instanceId, out LaunchedProcess launchedProcess) => _launchedProcesses.TryGetValue(instanceId, out launchedProcess!);

    public bool TryRemove(Guid instanceId, out LaunchedProcess launchedProcess) => _launchedProcesses.TryRemove(instanceId, out launchedProcess!);

    public IReadOnlyCollection<Guid> RunningInstanceIds => _launchedProcesses.Keys.ToArray();
}
