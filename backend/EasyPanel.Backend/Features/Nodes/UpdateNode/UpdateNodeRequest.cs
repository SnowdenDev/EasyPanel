using EasyPanel.Backend.Infrastructure.Entities;

namespace EasyPanel.Backend.Features.Nodes.UpdateNode;

// Deliberately excludes the node token — rotating it is a distinct, not-yet-built
// operation (see architecture.md's known gaps), not something folded into a rename/
// reconfigure edit.
public sealed record UpdateNodeRequest(string DisplayName, NodeConnectivityMode ConnectivityMode);
