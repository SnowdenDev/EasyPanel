namespace EasyPanel.Backend.Features.Staff.AssignServerPermissions;

public sealed record AssignServerPermissionsRequest(
    bool CanViewConsole,
    bool CanSendConsoleInput,
    bool CanControlPower,
    bool CanAccessFileManager,
    bool CanEditSettings
);
