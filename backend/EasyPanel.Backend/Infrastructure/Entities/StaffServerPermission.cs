namespace EasyPanel.Backend.Infrastructure.Entities;

/// <summary>
/// Per-server permission grant for a Staff user. Admin users bypass this table entirely
/// in the authorization policy — it only ever applies to the Staff role.
/// </summary>
public sealed class StaffServerPermission
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid InstanceId { get; set; }
    public bool CanViewConsole { get; set; }
    public bool CanSendConsoleInput { get; set; }
    public bool CanControlPower { get; set; }
    public bool CanAccessFileManager { get; set; }
    public bool CanEditSettings { get; set; }
}
