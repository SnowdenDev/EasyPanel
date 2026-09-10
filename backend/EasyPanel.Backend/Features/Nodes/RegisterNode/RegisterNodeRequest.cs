using EasyPanel.Backend.Infrastructure.Entities;

namespace EasyPanel.Backend.Features.Nodes.RegisterNode;

public sealed record RegisterNodeRequest(string DisplayName, NodeConnectivityMode ConnectivityMode);
