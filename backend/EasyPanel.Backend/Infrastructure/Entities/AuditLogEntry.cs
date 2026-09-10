namespace EasyPanel.Backend.Infrastructure.Entities;

public sealed class AuditLogEntry
{
    public Guid Id { get; set; }

    /// <summary>Null when the entry was written by the system or the daemon, not a logged-in user.</summary>
    public Guid? ActorUserId { get; set; }

    public Guid? InstanceId { get; set; }
    public Guid? NodeId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? DetailsJson { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
