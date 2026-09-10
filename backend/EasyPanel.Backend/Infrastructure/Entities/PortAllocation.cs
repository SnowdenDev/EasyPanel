namespace EasyPanel.Backend.Infrastructure.Entities;

public sealed class PortAllocation
{
    public Guid Id { get; set; }
    public Guid InstanceId { get; set; }
    public Guid NodeId { get; set; }
    public int Port { get; set; }
    public PortProtocol Protocol { get; set; }
    public string? Label { get; set; }
}
