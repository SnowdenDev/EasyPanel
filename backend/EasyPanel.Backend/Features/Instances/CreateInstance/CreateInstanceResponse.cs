using EasyPanel.Contracts.Enums;

namespace EasyPanel.Backend.Features.Instances.CreateInstance;

public sealed record CreateInstanceResponse(Guid InstanceId, InstanceStatus Status);
