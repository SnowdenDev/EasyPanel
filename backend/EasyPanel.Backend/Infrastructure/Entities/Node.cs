namespace EasyPanel.Backend.Infrastructure.Entities;

public sealed class Node
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// SHA256 of the node's raw auth token. The raw token itself is shown to the admin
    /// exactly once, at registration time, and is never stored or retrievable again.
    /// </summary>
    public string NodeTokenHash { get; set; } = string.Empty;

    public NodeConnectivityMode ConnectivityMode { get; set; }
    public bool IsOnline { get; set; }
    public DateTimeOffset? LastHeartbeatAtUtc { get; set; }
    public string? DaemonVersion { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
