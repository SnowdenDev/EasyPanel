using System.Security.Cryptography;
using EasyPanel.Daemon.Features.LaunchInstance;
using Xunit;

namespace EasyPanel.Daemon.Tests.Features.LaunchInstance;

public sealed class ExecutableHashVerifierTests : IDisposable
{
    private readonly string _filePath = Path.Combine(
        Path.GetTempPath(),
        "EasyPanelHashVerifierTests_" + Guid.NewGuid());

    private readonly byte[] _approvedBytes = "the exact bytes an admin approved"u8.ToArray();

    public ExecutableHashVerifierTests()
    {
        File.WriteAllBytes(_filePath, _approvedBytes);
    }

    private string ExpectedHashHex() => Convert.ToHexString(SHA256.HashData(_approvedBytes));

    [Fact]
    public async Task MatchesExpectedHashAsync_ReturnsTrue_ForTheCorrectHash()
    {
        var matches = await ExecutableHashVerifier.MatchesExpectedHashAsync(
            _filePath,
            ExpectedHashHex(),
            CancellationToken.None);

        Assert.True(matches);
    }

    [Fact]
    public async Task MatchesExpectedHashAsync_IsCaseInsensitive_TowardTheExpectedHash()
    {
        // Convert.ToHexString emits uppercase, but an admin may paste a hash in lowercase
        // (that's what certutil/sha256sum print) — a case difference must not refuse a launch.
        var matches = await ExecutableHashVerifier.MatchesExpectedHashAsync(
            _filePath,
            ExpectedHashHex().ToLowerInvariant(),
            CancellationToken.None);

        Assert.True(matches);
    }

    [Fact]
    public async Task MatchesExpectedHashAsync_ReturnsFalse_WhenTheFileDoesNotMatchTheExpectedHash()
    {
        var hashOfDifferentBytes = Convert.ToHexString(SHA256.HashData("a tampered binary"u8.ToArray()));

        var matches = await ExecutableHashVerifier.MatchesExpectedHashAsync(
            _filePath,
            hashOfDifferentBytes,
            CancellationToken.None);

        Assert.False(matches);
    }

    [Fact]
    public async Task MatchesExpectedHashAsync_ReturnsFalse_WhenTheExpectedHashIsNotEvenAHash()
    {
        var matches = await ExecutableHashVerifier.MatchesExpectedHashAsync(
            _filePath,
            "not-a-real-hash",
            CancellationToken.None);

        Assert.False(matches);
    }

    [Fact]
    public async Task ComputeHashAsync_ProducesTheKnownUppercaseSha256HexOfItsContents()
    {
        // Pin the well-known SHA256("abc") so a future refactor can't silently change the
        // algorithm or the hex encoding the whole safety guarantee is compared against.
        File.WriteAllBytes(_filePath, "abc"u8.ToArray());

        var hash = await ExecutableHashVerifier.ComputeHashAsync(_filePath, CancellationToken.None);

        Assert.Equal("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD", hash);
    }

    public void Dispose()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }
}
