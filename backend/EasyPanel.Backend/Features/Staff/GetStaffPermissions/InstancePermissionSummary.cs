namespace EasyPanel.Backend.Features.Staff.GetStaffPermissions;

public sealed record InstancePermissionSummary(
    Guid InstanceId,
    string InstanceDisplayName,
    bool CanViewConsole,
    bool CanSendConsoleInput,
    bool CanControlPower,
    bool CanAccessFileManager,
    bool CanEditSettings);
