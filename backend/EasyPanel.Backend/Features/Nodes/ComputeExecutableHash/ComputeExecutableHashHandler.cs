using EasyPanel.Backend.Infrastructure;
using EasyPanel.Contracts.Control.BackendToDaemon;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Features.Nodes.ComputeExecutableHash;

public sealed class ComputeExecutableHashHandler(
    AppDbContext dbContext,
    IHubContext<DaemonControlHub> daemonControlHub,
    PendingHashComputationTracker tracker)
{
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(15);

    public async Task<ComputeExecutableHashResult> HandleAsync(Guid nodeId, ComputeExecutableHashRequest request, CancellationToken cancellationToken)
    {
        var node = await dbContext.Nodes.SingleOrDefaultAsync(candidate => candidate.Id == nodeId, cancellationToken);
        if (node is null)
        {
            return new ComputeExecutableHashResult(false, null, $"No node with id '{nodeId}' exists.");
        }

        if (!node.IsOnline)
        {
            return new ComputeExecutableHashResult(false, null, "Node is offline.");
        }

        var correlationId = Guid.NewGuid();
        var pendingResult = tracker.RegisterAsync(nodeId, correlationId);

        var command = new ComputeExecutableHashCommand(correlationId, request.WorkDirectory, request.ExecutableRelativePath);
        await daemonControlHub.Clients.Group(HubGroupNames.NodeGroup(nodeId)).SendAsync("ComputeExecutableHash", command, cancellationToken);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(ResponseTimeout);

        try
        {
            await using var registration = timeoutCts.Token.Register(() => tracker.Cancel(correlationId));
            var result = await pendingResult.WaitAsync(timeoutCts.Token);

            return result.Succeeded
                ? new ComputeExecutableHashResult(true, result.Sha256Hex, null)
                : new ComputeExecutableHashResult(false, null, result.FailureReason);
        }
        catch (OperationCanceledException)
        {
            return new ComputeExecutableHashResult(false, null, "Timed out waiting for the node to respond.");
        }
    }
}
