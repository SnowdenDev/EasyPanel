using EasyPanel.Contracts.Enums;

namespace EasyPanel.Backend.Features.Instances.ListInstances;

public sealed record InstanceSummary(
    Guid Id,
    string DisplayName,
    Guid NodeId,
    string NodeDisplayName,
    InstanceStatus Status
);
