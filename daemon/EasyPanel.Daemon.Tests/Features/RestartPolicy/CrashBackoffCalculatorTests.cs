using EasyPanel.Daemon.Features.RestartPolicy;
using Xunit;

namespace EasyPanel.Daemon.Tests.Features.RestartPolicy;

public sealed class CrashBackoffCalculatorTests
{
    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 5)]
    [InlineData(2, 10)]
    [InlineData(3, 30)]
    public void GetDelayForAttempt_FollowsTheDocumentedSchedule(int attemptNumber, int expectedSeconds)
    {
        var delay = CrashBackoffCalculator.GetDelayForAttempt(attemptNumber);

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), delay);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(10)]
    [InlineData(1000)]
    public void GetDelayForAttempt_StaysAtTheLastScheduledDelay_BeyondTheScheduleLength(int attemptNumber)
    {
        var delay = CrashBackoffCalculator.GetDelayForAttempt(attemptNumber);

        Assert.Equal(TimeSpan.FromSeconds(30), delay);
    }

    [Fact]
    public void GetDelayForAttempt_ClampsNegativeAttemptNumbersToTheFirstDelay()
    {
        var delay = CrashBackoffCalculator.GetDelayForAttempt(-5);

        Assert.Equal(TimeSpan.FromSeconds(2), delay);
    }
}
