using System.Security.Cryptography;

namespace EasyPanel.Daemon.Features.LaunchInstance;

/// <summary>
/// The daemon's core safety promise: never launch an executable whose contents don't
/// match what the admin declared. See docs/architecture.md.
/// </summary>
internal static class ExecutableHashVerifier
{
    public static async Task<bool> MatchesExpectedHashAsync(string executableFullPath, string expectedSha256Hex, CancellationToken cancellationToken)
    {
        var actualHashHex = await ComputeHashAsync(executableFullPath, cancellationToken);
        return string.Equals(actualHashHex, expectedSha256Hex, StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<string> ComputeHashAsync(string executableFullPath, CancellationToken cancellationToken)
    {
        await using var fileStream = File.OpenRead(executableFullPath);
        var hashBytes = await SHA256.HashDataAsync(fileStream, cancellationToken);
        return Convert.ToHexString(hashBytes);
    }
}
