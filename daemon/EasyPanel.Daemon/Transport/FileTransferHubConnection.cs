using EasyPanel.Contracts.FileTransfer;
using EasyPanel.Contracts.Serialization;
using EasyPanel.Daemon.Features.FileTransfer;
using EasyPanel.Daemon.Infrastructure;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasyPanel.Daemon.Transport;

/// <summary>
/// A second, independent outbound connection to /hubs/file-transfer — deliberately
/// separate from ControlHubConnection's connection to DaemonControlHub, so a large
/// transfer can never head-of-line-block console/command traffic. See docs/architecture.md.
///
/// Resolves FileTransferCommandHandler lazily via IServiceProvider for the same reason
/// ControlHubConnection resolves its handlers lazily — see that class's remarks.
/// </summary>
internal sealed class FileTransferHubConnection(
    IOptions<NodeIdentityOptions> nodeIdentityOptions,
    IServiceProvider serviceProvider,
    ILogger<FileTransferHubConnection> logger)
    : BackgroundService, IFileTransferReporter
{
    private static readonly TimeSpan[] AutomaticReconnectBackoff =
    [
        TimeSpan.Zero,
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30),
    ];

    private readonly NodeIdentityOptions _options = nodeIdentityOptions.Value;
    private HubConnection? _connection;

    private FileTransferCommandHandler CommandHandler => serviceProvider.GetRequiredService<FileTransferCommandHandler>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl($"{_options.BackendBaseUrl}/hubs/file-transfer?nodeId={_options.NodeId}", httpOptions =>
            {
                httpOptions.AccessTokenProvider = () => Task.FromResult<string?>(_options.RawNodeToken);
            })
            .WithAutomaticReconnect(AutomaticReconnectBackoff)
            .AddJsonProtocol(jsonOptions =>
            {
                jsonOptions.PayloadSerializerOptions.TypeInfoResolver = ContractsJsonContext.Default;
            })
            .Build();

        _connection.On<FileTransferRequest>("BeginFileDownload", request => CommandHandler.HandleBeginDownloadAsync(request, this));
        _connection.On<FileTransferRequest>("BeginFileUpload", request => CommandHandler.HandleBeginUploadAsync(request, this));
        _connection.On<FileChunk>("ReceiveFileChunk", chunk => CommandHandler.HandleReceiveChunkAsync(chunk, this));
        _connection.On<Guid>("CancelFileTransfer", transferId => CommandHandler.CancelDownload(transferId));

        _connection.Closed += error =>
        {
            logger.LogWarning(error, "File-transfer connection to backend closed — automatic reconnect exhausted, taking over manually.");
            return ReconnectLoopAsync(stoppingToken);
        };

        await StartWithRetryAsync(stoppingToken);

        // No heartbeat needed on this connection — DaemonControlHub's heartbeat already
        // tells the backend the node is alive; this connection only ever carries file data.
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private async Task StartWithRetryAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _connection!.StartAsync(cancellationToken);
                logger.LogInformation("File-transfer connection established for node {NodeId}.", _options.NodeId);
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to establish file-transfer connection, retrying in 5s.");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    private async Task ReconnectLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _connection!.State != HubConnectionState.Connected)
        {
            try
            {
                await _connection.StartAsync(cancellationToken);
                logger.LogInformation("File-transfer connection re-established.");
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Manual file-transfer reconnect attempt failed, retrying in 30s.");
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }
    }

    public Task ReportMetadataAsync(Guid transferId, long totalSizeBytes) =>
        InvokeIfConnectedAsync("ReportFileTransferMetadata", new FileTransferMetadata(transferId, totalSizeBytes));

    public Task SendChunkAsync(Guid transferId, long sequenceNumber, byte[] data, bool isFinal) =>
        InvokeIfConnectedAsync("SendFileChunk", new FileChunk(transferId, sequenceNumber, data, isFinal));

    public Task ReportResultAsync(Guid transferId, bool succeeded, string? failureReason) =>
        InvokeIfConnectedAsync("ReportFileTransferResult", new FileTransferResult(transferId, succeeded, failureReason));

    private async Task InvokeIfConnectedAsync(string methodName, object argument)
    {
        if (_connection is not { State: HubConnectionState.Connected })
        {
            logger.LogDebug("Skipped sending {Method} — file-transfer connection is not connected right now.", methodName);
            return;
        }

        try
        {
            await _connection.SendAsync(methodName, argument);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to send {Method} on the file-transfer connection.", methodName);
        }
    }
}
