namespace EasyPanel.Daemon.Infrastructure;

/// <summary>
/// The daemon is the actual filesystem owner, so it never trusts a relative path from the
/// wire at face value — even though the backend's own validator already rejects "..", a
/// compromised or buggy backend shouldn't be able to make the daemon touch anything
/// outside an instance's work directory. See docs/architecture.md.
/// </summary>
internal static class PathTraversalGuard
{
    public static bool TryResolveSafePath(string rootDirectory, string relativePath, out string resolvedFullPath)
    {
        resolvedFullPath = string.Empty;

        // Path.Combine silently discards the first argument if the second is rooted
        // (absolute, drive-qualified, or UNC) — reject that case explicitly rather than
        // relying only on the StartsWith check below to catch it after the fact.
        if (Path.IsPathRooted(relativePath))
        {
            return false;
        }

        var rootFullPath = Path.GetFullPath(rootDirectory);
        var candidateFullPath = Path.GetFullPath(Path.Combine(rootFullPath, relativePath));

        var staysUnderRoot = candidateFullPath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase)
            && (candidateFullPath.Length == rootFullPath.Length || candidateFullPath[rootFullPath.Length] == Path.DirectorySeparatorChar);

        if (!staysUnderRoot)
        {
            return false;
        }

        // A symlink that lives inside the root but points outside it is still an escape.
        if (File.Exists(candidateFullPath))
        {
            var linkTarget = File.ResolveLinkTarget(candidateFullPath, returnFinalTarget: true);
            if (linkTarget is not null && !Path.GetFullPath(linkTarget.FullName).StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        resolvedFullPath = candidateFullPath;
        return true;
    }
}
