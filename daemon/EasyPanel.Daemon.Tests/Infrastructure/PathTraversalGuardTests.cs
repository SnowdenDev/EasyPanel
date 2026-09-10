using Xunit;

namespace EasyPanel.Daemon.Tests.Infrastructure;

public sealed class PathTraversalGuardTests
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "EasyPanelPathGuardTests_" + Guid.NewGuid());

    public PathTraversalGuardTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void TryResolveSafePath_Succeeds_ForAPlainRelativePath()
    {
        var succeeded = EasyPanel.Daemon.Infrastructure.PathTraversalGuard.TryResolveSafePath(_root, "server.exe", out var resolved);

        Assert.True(succeeded);
        Assert.Equal(Path.GetFullPath(Path.Combine(_root, "server.exe")), resolved);
    }

    [Fact]
    public void TryResolveSafePath_Succeeds_ForANestedRelativePath()
    {
        var succeeded = EasyPanel.Daemon.Infrastructure.PathTraversalGuard.TryResolveSafePath(_root, Path.Combine("bin", "server.exe"), out var resolved);

        Assert.True(succeeded);
        Assert.StartsWith(Path.GetFullPath(_root), resolved, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("..\\escape.exe")]
    [InlineData("..\\..\\Windows\\System32\\cmd.exe")]
    [InlineData("subdir\\..\\..\\escape.exe")]
    public void TryResolveSafePath_Fails_ForDotDotEscapes(string maliciousRelativePath)
    {
        var succeeded = EasyPanel.Daemon.Infrastructure.PathTraversalGuard.TryResolveSafePath(_root, maliciousRelativePath, out _);

        Assert.False(succeeded);
    }

    [Fact]
    public void TryResolveSafePath_Fails_ForAnAbsolutePath()
    {
        var succeeded = EasyPanel.Daemon.Infrastructure.PathTraversalGuard.TryResolveSafePath(_root, "C:\\Windows\\System32\\cmd.exe", out _);

        Assert.False(succeeded);
    }

    [Fact]
    public void TryResolveSafePath_Fails_ForAUncPath()
    {
        var succeeded = EasyPanel.Daemon.Infrastructure.PathTraversalGuard.TryResolveSafePath(_root, "\\\\server\\share\\evil.exe", out _);

        Assert.False(succeeded);
    }

    [Fact]
    public void TryResolveSafePath_Fails_ForASiblingDirectoryThatMerelySharesAPrefix()
    {
        // e.g. root "C:\Servers\Valheim01" must not accept something resolving into
        // "C:\Servers\Valheim01Evil" just because the string happens to start the same way.
        var siblingWithSharedPrefix = _root + "Evil";
        Directory.CreateDirectory(siblingWithSharedPrefix);

        try
        {
            var relativePathThatWouldEscape = Path.GetRelativePath(_root, Path.Combine(siblingWithSharedPrefix, "evil.exe"));
            var succeeded = EasyPanel.Daemon.Infrastructure.PathTraversalGuard.TryResolveSafePath(_root, relativePathThatWouldEscape, out _);

            Assert.False(succeeded);
        }
        finally
        {
            Directory.Delete(siblingWithSharedPrefix, recursive: true);
        }
    }
}
