using EasyPanel.Contracts.Control.BackendToDaemon;
using EasyPanel.Contracts.Enums;
using EasyPanel.Daemon.Features.LaunchInstance;
using EasyPanel.Daemon.Transport;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EasyPanel.Daemon.Tests.Features.LaunchInstance;

public sealed class LaunchInstanceCommandHandlerTests : IDisposable
{
    private readonly string _workDirectory = Path.Combine(
        Path.GetTempPath(),
        "EasyPanelLaunchTests_" + Guid.NewGuid());

    public LaunchInstanceCommandHandlerTests()
    {
        Directory.CreateDirectory(_workDirectory);
    }

    [Fact]
    public async Task HandleAsync_RefusesToLaunch_WhenTheExecutableHashDoesNotMatch()
    {
        // The core safety guarantee: an executable whose bytes don't match the admin-declared
        // hash must never be launched. The file exists and its path is valid — only the hash
        // is wrong — so this isolates the SHA256 gate from the path and existence checks.
        const string executableRelativePath = "server.exe";
        File.WriteAllBytes(Path.Combine(_workDirectory, executableRelativePath), "a tampered binary"u8.ToArray());

        var registry = new LaunchedProcessRegistry();
        var reporter = new RecordingBackendReporter();
        var handler = new LaunchInstanceCommandHandler(registry, reporter, NullLogger<LaunchInstanceCommandHandler>.Instance);

        var command = CreateLaunchCommand(
            executableRelativePath,
            expectedSha256: "0000000000000000000000000000000000000000000000000000000000000000");

        await handler.HandleAsync(command, CancellationToken.None);

        Assert.Empty(registry.RunningInstanceIds);
        var report = Assert.Single(reporter.StatusChanges);
        Assert.Equal(command.InstanceId, report.InstanceId);
        Assert.Equal(InstanceStatus.HashMismatchRefused, report.Status);
    }

    private LaunchInstanceCommand CreateLaunchCommand(string executableRelativePath, string expectedSha256) => new(
        InstanceId: Guid.NewGuid(),
        WorkDirectory: _workDirectory,
        ExecutableRelativePath: executableRelativePath,
        ExpectedSha256: expectedSha256,
        LaunchArguments: null,
        EnvironmentVariables: new Dictionary<string, string>(),
        CpuLimitPercent: null,
        MemoryLimitMegabytes: null,
        AutoRestartEnabled: false);

    public void Dispose()
    {
        if (Directory.Exists(_workDirectory))
        {
            Directory.Delete(_workDirectory, recursive: true);
        }
    }

    private sealed class RecordingBackendReporter : IBackendReporter
    {
        public List<(Guid InstanceId, InstanceStatus Status, int? ExitCode)> StatusChanges { get; } = [];

        public Task ReportInstanceStatusChangedAsync(Guid instanceId, InstanceStatus newStatus, int? exitCode, CancellationToken cancellationToken)
        {
            StatusChanges.Add((instanceId, newStatus, exitCode));
            return Task.CompletedTask;
        }

        public Task ReportConsoleOutputLineAsync(Guid instanceId, ConsoleStreamKind streamKind, string text, long sequenceNumber, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ReportInstanceCrashedAsync(Guid instanceId, int exitCode, bool willAutoRestart, DateTimeOffset? nextRestartAttemptUtc, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ReportExecutableHashComputedAsync(Guid correlationId, bool succeeded, string? sha256Hex, string? failureReason, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
