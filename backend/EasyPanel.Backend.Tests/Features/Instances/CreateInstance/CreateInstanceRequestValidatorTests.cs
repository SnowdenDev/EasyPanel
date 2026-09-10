using EasyPanel.Backend.Features.Instances.CreateInstance;
using Xunit;

namespace EasyPanel.Backend.Tests.Features.Instances.CreateInstance;

public sealed class CreateInstanceRequestValidatorTests
{
    private readonly CreateInstanceRequestValidator _validator = new();

    private static CreateInstanceRequest ValidRequest(
        string workDirectory = "D:/GameServers/Valheim01",
        string executableRelativePath = "valheim_server.exe",
        string expectedExecutableSha256 = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")
    {
        return new CreateInstanceRequest(
            Guid.NewGuid(),
            "Valheim Test",
            workDirectory,
            executableRelativePath,
            expectedExecutableSha256,
            LaunchArguments: null,
            EnvironmentVariables: null,
            CpuLimitPercent: null,
            MemoryLimitMegabytes: null);
    }

    [Fact]
    public void Validate_Succeeds_ForAWellFormedRequest()
    {
        var result = _validator.Validate(ValidRequest());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Fails_WhenWorkDirectoryIsNotAbsolute()
    {
        var result = _validator.Validate(ValidRequest(workDirectory: "relative/path"));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("../escape.exe")]
    [InlineData("subdir/../../escape.exe")]
    [InlineData("/absolute/escape.exe")]
    public void Validate_Fails_WhenExecutablePathTriesToEscapeTheWorkDirectory(string maliciousPath)
    {
        var result = _validator.Validate(ValidRequest(executableRelativePath: maliciousPath));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("too-short")]
    [InlineData("")]
    [InlineData("GGGGAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")] // 'G' is not valid hex
    public void Validate_Fails_ForAMalformedSha256(string malformedHash)
    {
        var result = _validator.Validate(ValidRequest(expectedExecutableSha256: malformedHash));

        Assert.False(result.IsValid);
    }
}
