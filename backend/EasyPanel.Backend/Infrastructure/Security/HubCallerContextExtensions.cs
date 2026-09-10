using Microsoft.AspNetCore.SignalR;

namespace EasyPanel.Backend.Infrastructure.Security;

public static class HubCallerContextExtensions
{
    public static Guid GetNodeId(this HubCallerContext context)
    {
        var claim = context.User?.FindFirst(NodeClaimTypes.NodeId)
            ?? throw new InvalidOperationException("This hub connection has no node_id claim — it wasn't authenticated as a node.");

        return Guid.Parse(claim.Value);
    }
}
