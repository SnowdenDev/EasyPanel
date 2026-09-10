using EasyPanel.Backend.Infrastructure.Security;
using EasyPanel.Contracts.FileTransfer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EasyPanel.Backend.Infrastructure;

/// <summary>
/// A second, independent outbound connection from the daemon, dedicated to chunked file
/// transfer so a large transfer can never head-of-line-block console/command traffic on
/// DaemonControlHub. See docs/architecture.md's file transfer section.
/// </summary>
[Authorize(AuthenticationSchemes = NodeTokenAuthenticationDefaults.SchemeName)]
public sealed class FileTransferHub(FileTransferCoordinator coordinator) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var nodeId = Context.GetNodeId();
        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroupNames.NodeGroup(nodeId));
        await base.OnConnectedAsync();
    }

    public void ReportFileTransferMetadata(FileTransferMetadata metadata) =>
        coordinator.PushMetadata(metadata.TransferId, metadata.TotalSizeBytes);

    public void SendFileChunk(FileChunk chunk) =>
        coordinator.PushChunk(chunk.TransferId, chunk.Bytes, chunk.IsFinal);

    public void ReportFileTransferResult(FileTransferResult result) =>
        coordinator.PushResult(result.TransferId, result.Succeeded, result.FailureReason);
}
