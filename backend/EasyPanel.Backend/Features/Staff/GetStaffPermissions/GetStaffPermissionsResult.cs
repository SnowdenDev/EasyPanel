namespace EasyPanel.Backend.Features.Staff.GetStaffPermissions;

public sealed record GetStaffPermissionsResult(bool Succeeded, string? ErrorMessage, IReadOnlyList<InstancePermissionSummary>? Permissions);
