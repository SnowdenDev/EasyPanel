using System.Diagnostics;

namespace EasyPanel.Daemon.Features.ConsoleStreaming;

internal static class StdinForwarder
{
    public static async Task SendAsync(Process process, string text, CancellationToken cancellationToken)
    {
        await process.StandardInput.WriteLineAsync(text.AsMemory(), cancellationToken);
        await process.StandardInput.FlushAsync(cancellationToken);
    }
}
