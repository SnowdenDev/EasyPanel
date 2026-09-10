using System.Diagnostics;
using EasyPanel.Contracts.Enums;
using EasyPanel.Daemon.Transport;

namespace EasyPanel.Daemon.Features.ConsoleStreaming;

/// <summary>One of these per launched process — hooks Process.OutputDataReceived/ErrorDataReceived and forwards each line to the backend.</summary>
internal sealed class ConsoleOutputPump(Guid instanceId, IBackendReporter reporter)
{
    private long _sequenceNumber;

    public void AttachTo(Process process)
    {
        process.OutputDataReceived += (_, e) => Forward(ConsoleStreamKind.StandardOutput, e.Data);
        process.ErrorDataReceived += (_, e) => Forward(ConsoleStreamKind.StandardError, e.Data);
    }

    private void Forward(ConsoleStreamKind streamKind, string? text)
    {
        // A null Data value is the .NET signal that the stream closed — not an empty line.
        if (text is null)
        {
            return;
        }

        var sequenceNumber = Interlocked.Increment(ref _sequenceNumber);

        // Fire-and-forget by design — a slow or momentarily disconnected backend
        // connection must never block the process's own output pipe.
        _ = reporter.ReportConsoleOutputLineAsync(instanceId, streamKind, text, sequenceNumber, CancellationToken.None);
    }
}
