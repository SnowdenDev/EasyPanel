namespace EasyPanel.Daemon.Features.RestartPolicy;

/// <summary>Same shape as the SignalR reconnect backoff — see docs/architecture.md.</summary>
internal static class CrashBackoffCalculator
{
    private static readonly TimeSpan[] Schedule =
    [
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30),
    ];

    public static TimeSpan GetDelayForAttempt(int attemptNumber)
    {
        var index = Math.Clamp(attemptNumber, 0, Schedule.Length - 1);
        return Schedule[index];
    }
}
