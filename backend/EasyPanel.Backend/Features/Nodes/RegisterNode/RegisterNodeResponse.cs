using EasyPanel.Backend.Infrastructure.Entities;

namespace EasyPanel.Backend.Features.Nodes.RegisterNode;

/// <summary>
/// RawNodeToken is shown here exactly once. The daemon needs it to connect; the backend
/// only ever stores its SHA256 hash afterward and cannot show it again.
/// </summary>
public sealed record RegisterNodeResponse(Guid NodeId, string RawNodeToken, NodeConnectivityMode ConnectivityMode);
