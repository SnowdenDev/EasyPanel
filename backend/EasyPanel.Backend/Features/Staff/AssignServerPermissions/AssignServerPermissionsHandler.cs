using System.Text.Json;
using EasyPanel.Backend.Infrastructure;
using EasyPanel.Backend.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Features.Staff.AssignServerPermissions;

public sealed class AssignServerPermissionsHandler(AppDbContext dbContext)
{
    public async Task<AssignServerPermissionsResult> HandleAsync(
        Guid targetUserId,
        Guid instanceId,
        AssignServerPermissionsRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var targetUser = await dbContext.Users.SingleOrDefaultAsync(user => user.Id == targetUserId, cancellationToken);
        if (targetUser is null)
        {
            return new AssignServerPermissionsResult(false, $"No user with id '{targetUserId}' exists.");
        }

        if (targetUser.Role != UserRole.Staff)
        {
            return new AssignServerPermissionsResult(false, "Per-server permissions only apply to Staff users — Admin already has full access.");
        }

        var instanceExists = await dbContext.Instances.AnyAsync(instance => instance.Id == instanceId, cancellationToken);
        if (!instanceExists)
        {
            return new AssignServerPermissionsResult(false, $"No instance with id '{instanceId}' exists.");
        }

        var permission = await dbContext.StaffServerPermissions
            .SingleOrDefaultAsync(candidate => candidate.UserId == targetUserId && candidate.InstanceId == instanceId, cancellationToken);

        if (permission is null)
        {
            permission = new StaffServerPermission { Id = Guid.NewGuid(), UserId = targetUserId, InstanceId = instanceId };
            dbContext.StaffServerPermissions.Add(permission);
        }

        permission.CanViewConsole = request.CanViewConsole;
        permission.CanSendConsoleInput = request.CanSendConsoleInput;
        permission.CanControlPower = request.CanControlPower;
        permission.CanAccessFileManager = request.CanAccessFileManager;
        permission.CanEditSettings = request.CanEditSettings;

        dbContext.AuditLogEntries.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            InstanceId = instanceId,
            Action = "StaffPermissionsChanged",
            DetailsJson = JsonSerializer.Serialize(new { targetUserId, request }),
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AssignServerPermissionsResult(true, null);
    }
}
