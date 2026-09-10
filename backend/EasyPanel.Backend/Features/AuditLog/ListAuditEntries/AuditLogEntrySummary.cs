namespace EasyPanel.Backend.Features.AuditLog.ListAuditEntries;

public sealed record AuditLogEntrySummary(
    Guid Id,
    Guid? ActorUserId,
    Guid? InstanceId,
    Guid? NodeId,
    string Action,
    string? DetailsJson,
    DateTimeOffset CreatedAtUtc
);
