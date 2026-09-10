namespace EasyPanel.Backend.Infrastructure;

public static class HubGroupNames
{
    public static string NodeGroup(Guid nodeId) => $"node-{nodeId}";

    public static string InstanceConsoleGroup(Guid instanceId) => $"instance-console-{instanceId}";
}
