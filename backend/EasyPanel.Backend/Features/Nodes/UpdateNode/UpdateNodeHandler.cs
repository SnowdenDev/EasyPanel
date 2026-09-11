using EasyPanel.Backend.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Features.Nodes.UpdateNode;

public sealed class UpdateNodeHandler(AppDbContext dbContext)
{
    public async Task<UpdateNodeResult> HandleAsync(Guid nodeId, UpdateNodeRequest request, CancellationToken cancellationToken)
    {
        var node = await dbContext.Nodes.SingleOrDefaultAsync(candidate => candidate.Id == nodeId, cancellationToken);
        if (node is null)
        {
            return new UpdateNodeResult(false, $"No node with id '{nodeId}' exists.");
        }

        // Changing ConnectivityMode after the fact is honest and expected — an admin who
        // registered a node as Remote by mistake (or moved it onto the same LAN since)
        // should be able to fix that without re-registering, which would also orphan
        // every instance already pointed at the old node.
        node.DisplayName = request.DisplayName;
        node.ConnectivityMode = request.ConnectivityMode;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateNodeResult(true, null);
    }
}
