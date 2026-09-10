namespace EasyPanel.Backend.Infrastructure.Entities;

/// <summary>
/// Purely a backend policy concept, not part of the daemon wire protocol — the daemon
/// never needs to know whether its own node is Local or Remote, it just streams what
/// it's told to. This is why it lives here and not in EasyPanel.Contracts (see the file
/// transfer section of docs/architecture.md).
/// </summary>
public enum NodeConnectivityMode
{
    Local,
    Remote,
}
