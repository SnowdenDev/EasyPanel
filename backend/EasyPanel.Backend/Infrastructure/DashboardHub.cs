using EasyPanel.Backend.Infrastructure.Security;
using EasyPanel.Contracts.Control.BackendToDaemon;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Infrastructure;

/// <summary>
/// Browser-to-backend channel for live console streaming and status pushes. Standard
/// JWT bearer auth — no bespoke scheme needed here, unlike the daemon hubs.
/// </summary>
[Authorize]
public sealed class DashboardHub(AppDbContext dbContext, IServerPermissionChecker permissionChecker, IHubContext<DaemonControlHub> daemonControlHub) : Hub
{
    public async Task SubscribeToInstanceConsole(Guid instanceId)
    {
        var canView = await permissionChecker.HasPermissionAsync(Context.User!, instanceId, ServerPermissionKind.ViewConsole, Context.ConnectionAborted);
        if (!canView)
        {
            throw new HubException("You do not have permission to view this instance's console.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroupNames.InstanceConsoleGroup(instanceId));
    }

    public async Task UnsubscribeFromInstanceConsole(Guid instanceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroupNames.InstanceConsoleGroup(instanceId));
    }

    public async Task SendConsoleCommand(Guid instanceId, string commandText)
    {
        var canSendInput = await permissionChecker.HasPermissionAsync(Context.User!, instanceId, ServerPermissionKind.SendConsoleInput, Context.ConnectionAborted);
        if (!canSendInput)
        {
            throw new HubException("You do not have permission to send console input to this instance.");
        }

        var instance = await dbContext.Instances.SingleAsync(candidate => candidate.Id == instanceId);

        await daemonControlHub.Clients.Group(HubGroupNames.NodeGroup(instance.NodeId))
            .SendAsync("SendConsoleInput", new SendConsoleInputCommand(instanceId, commandText));
    }
}
