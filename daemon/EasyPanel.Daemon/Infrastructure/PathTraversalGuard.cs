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

        if (string.IsNullOrWhiteSpace(rootDirectory)
            || string.IsNullOrWhiteSpace(relativePath)
            || Path.IsPathRooted(relativePath))
        {
            return false;
        }

        try
        {
            var rootFullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootDirectory));
            var candidateFullPath = Path.GetFullPath(Path.Combine(rootFullPath, relativePath));
            var rootPrefix = Path.EndsInDirectorySeparator(rootFullPath)
                ? rootFullPath
                : rootFullPath + Path.DirectorySeparatorChar;

            var staysUnderRoot = candidateFullPath.Equals(rootFullPath, StringComparison.OrdinalIgnoreCase)
                || candidateFullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase);

            if (!staysUnderRoot || ContainsReparsePoint(rootFullPath, candidateFullPath))
            {
                return false;
            }

            resolvedFullPath = candidateFullPath;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or NotSupportedException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool ContainsReparsePoint(string rootFullPath, string candidateFullPath)
    {
        if (IsExistingReparsePoint(rootFullPath))
        {
            return true;
        }

        var relativePath = Path.GetRelativePath(rootFullPath, candidateFullPath);
        var currentPath = rootFullPath;

        foreach (var segment in relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (segment.Length == 0 || segment == ".")
            {
                continue;
            }

            currentPath = Path.Combine(currentPath, segment);
            if (!File.Exists(currentPath) && !Directory.Exists(currentPath))
            {
                break;
            }

            if (IsExistingReparsePoint(currentPath))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsExistingReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
}
