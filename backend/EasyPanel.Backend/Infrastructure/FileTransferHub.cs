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

    public async Task ReportFileTransferMetadata(FileTransferMetadata metadata)
    {
        var nodeId = Context.GetNodeId();
        await coordinator.PushMetadataAsync(nodeId, metadata.TransferId, metadata.TotalSizeBytes, Context.ConnectionAborted);
    }

    public async Task SendFileChunk(FileChunk chunk)
    {
        var nodeId = Context.GetNodeId();
        await coordinator.PushChunkAsync(nodeId, chunk.TransferId, chunk.Bytes, chunk.IsFinal, Context.ConnectionAborted);
    }

    public async Task ReportFileTransferResult(FileTransferResult result)
    {
        var nodeId = Context.GetNodeId();
        await coordinator.PushResultAsync(nodeId, result.TransferId, result.Succeeded, result.FailureReason, Context.ConnectionAborted);
    }
}
