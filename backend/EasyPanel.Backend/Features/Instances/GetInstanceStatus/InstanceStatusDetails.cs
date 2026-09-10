using EasyPanel.Contracts.Enums;

namespace EasyPanel.Backend.Features.Instances.GetInstanceStatus;

public sealed record InstanceStatusDetails(
    Guid InstanceId,
    string DisplayName,
    InstanceStatus Status,
    Guid NodeId,
    bool IsNodeOnline,
    DateTimeOffset UpdatedAtUtc
);
