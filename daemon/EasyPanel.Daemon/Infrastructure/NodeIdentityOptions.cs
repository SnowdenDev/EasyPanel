namespace EasyPanel.Daemon.Infrastructure;

/// <summary>Read from appsettings.json / environment variables — see appsettings.json for the shape.</summary>
public sealed class NodeIdentityOptions
{
    public const string SectionName = "NodeIdentity";

    public Guid NodeId { get; set; }
    public string RawNodeToken { get; set; } = string.Empty;
    public string BackendBaseUrl { get; set; } = string.Empty;
    public string DaemonVersion { get; set; } = "0.1.0-dev";
    public int HeartbeatIntervalSeconds { get; set; } = 15;
}
