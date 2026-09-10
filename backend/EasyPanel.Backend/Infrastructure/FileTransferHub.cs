using EasyPanel.Backend.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EasyPanel.Backend.Infrastructure;

/// <summary>
/// Skeleton for now — a second, independent outbound connection from the daemon,
/// dedicated to chunked file transfer so a large transfer can never head-of-line-block
/// console/command traffic on DaemonControlHub. The chunk-relay methods
/// (SendFileChunk/ReceiveFileChunk and the REST endpoints that bridge them to browser
/// HTTP streams) land in Phase 2, alongside the daemon's file transfer feature — see
/// docs/architecture.md.
/// </summary>
[Authorize(AuthenticationSchemes = NodeTokenAuthenticationDefaults.SchemeName)]
public sealed class FileTransferHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var nodeId = Context.GetNodeId();
        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroupNames.NodeGroup(nodeId));
        await base.OnConnectedAsync();
    }
}
