using System.Collections.Concurrent;
using EasyPanel.Contracts.FileTransfer;
using EasyPanel.Daemon.Infrastructure;
using Microsoft.Extensions.Logging;

namespace EasyPanel.Daemon.Features.FileTransfer;

/// <summary>
/// The actual file I/O for downloads and uploads. PathTraversalGuard resolves every path
/// against the instance's work directory before anything touches disk — the daemon is the
/// real filesystem owner and never trusts a relative path from the wire at face value, even
/// though the backend's own validator already blocks "..". See docs/architecture.md.
/// </summary>
internal sealed class FileTransferCommandHandler(TokenBucketThrottle throttle, ILogger<FileTransferCommandHandler> logger)
{
    private const int ChunkSizeBytes = 64 * 1024;

    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _activeDownloads = new();
    private readonly ConcurrentDictionary<Guid, UploadState> _activeUploads = new();

    public async Task HandleBeginDownloadAsync(FileTransferRequest request, IFileTransferReporter reporter)
    {
        if (!PathTraversalGuard.TryResolveSafePath(request.WorkDirectory, request.RelativePath, out var fullPath) || !File.Exists(fullPath))
        {
            await reporter.ReportResultAsync(request.TransferId, false, "File not found, or the path escapes the instance's work directory.");
            return;
        }

        var cancellationSource = new CancellationTokenSource();
        _activeDownloads[request.TransferId] = cancellationSource;

        try
        {
            var fileInfo = new FileInfo(fullPath);
            await reporter.ReportMetadataAsync(request.TransferId, fileInfo.Length);

            await using var fileStream = File.OpenRead(fullPath);

            if (fileInfo.Length == 0)
            {
                await reporter.SendChunkAsync(request.TransferId, 0, [], isFinal: true);
                return;
            }

            var buffer = new byte[ChunkSizeBytes];
            long sequenceNumber = 0;
            int bytesRead;

            while ((bytesRead = await fileStream.ReadAsync(buffer, cancellationSource.Token)) > 0)
            {
                await throttle.ConsumeAsync(bytesRead, cancellationSource.Token);

                var chunkData = bytesRead == buffer.Length ? buffer.ToArray() : buffer[..bytesRead];
                var isFinal = fileStream.Position >= fileInfo.Length;

                await reporter.SendChunkAsync(request.TransferId, sequenceNumber, chunkData, isFinal);
                sequenceNumber++;
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Download {TransferId} was cancelled.", request.TransferId);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Download {TransferId} failed.", request.TransferId);
            await reporter.ReportResultAsync(request.TransferId, false, exception.Message);
        }
        finally
        {
            _activeDownloads.TryRemove(request.TransferId, out _);
        }
    }

    public void CancelDownload(Guid transferId)
    {
        if (_activeDownloads.TryGetValue(transferId, out var cancellationSource))
        {
            cancellationSource.Cancel();
        }
    }

    public async Task HandleBeginUploadAsync(FileTransferRequest request, IFileTransferReporter reporter)
    {
        if (!PathTraversalGuard.TryResolveSafePath(request.WorkDirectory, request.RelativePath, out var finalFilePath))
        {
            await reporter.ReportResultAsync(request.TransferId, false, "The path escapes the instance's work directory.");
            return;
        }

        // Write to a temp file and rename into place on success — a failed or aborted
        // upload must never leave a half-written file visible under the real name.
        var tempFilePath = finalFilePath + ".uploading-" + Guid.NewGuid().ToString("N");

        try
        {
            var stream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            _activeUploads[request.TransferId] = new UploadState(stream, tempFilePath, finalFilePath);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to open temp file for upload {TransferId}.", request.TransferId);
            await reporter.ReportResultAsync(request.TransferId, false, exception.Message);
        }
    }

    public async Task HandleReceiveChunkAsync(FileChunk chunk, IFileTransferReporter reporter)
    {
        if (!_activeUploads.TryGetValue(chunk.TransferId, out var state))
        {
            return;
        }

        try
        {
            if (chunk.Bytes.Length > 0)
            {
                await throttle.ConsumeAsync(chunk.Bytes.Length, CancellationToken.None);
                await state.Stream.WriteAsync(chunk.Bytes);
            }

            if (!chunk.IsFinal)
            {
                return;
            }

            await state.Stream.FlushAsync();
            await state.Stream.DisposeAsync();
            File.Move(state.TempFilePath, state.FinalFilePath, overwrite: true);
            _activeUploads.TryRemove(chunk.TransferId, out _);

            await reporter.ReportResultAsync(chunk.TransferId, true, null);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Upload {TransferId} failed.", chunk.TransferId);

            if (_activeUploads.TryRemove(chunk.TransferId, out var failedState))
            {
                await failedState.Stream.DisposeAsync();
                TryDeleteTempFile(failedState.TempFilePath);
            }

            await reporter.ReportResultAsync(chunk.TransferId, false, exception.Message);
        }
    }

    private static void TryDeleteTempFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup — a leaked ".uploading-*" temp file is a cosmetic
            // annoyance, not a correctness problem, and isn't worth failing louder over.
        }
    }
}
