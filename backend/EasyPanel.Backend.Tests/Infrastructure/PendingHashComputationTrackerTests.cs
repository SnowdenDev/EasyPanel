using EasyPanel.Backend.Infrastructure;
using EasyPanel.Contracts.Control.DaemonToBackend;

namespace EasyPanel.Backend.Tests.Infrastructure;

public sealed class PendingHashComputationTrackerTests
{
    [Fact]
    public void Complete_RejectsAResponseFromAnotherNode()
    {
        var tracker = new PendingHashComputationTracker();
        var expectedNodeId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var pending = tracker.RegisterAsync(expectedNodeId, correlationId);
        var result = new HashComputationResult(correlationId, true, new string('A', 64), null);

        var completed = tracker.Complete(Guid.NewGuid(), result);

        Assert.False(completed);
        Assert.False(pending.IsCompleted);
    }

    [Fact]
    public async Task Complete_AcceptsTheResponseFromTheRegisteredNode()
    {
        var tracker = new PendingHashComputationTracker();
        var expectedNodeId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var pending = tracker.RegisterAsync(expectedNodeId, correlationId);
        var result = new HashComputationResult(correlationId, true, new string('A', 64), null);

        var completed = tracker.Complete(expectedNodeId, result);

        Assert.True(completed);
        Assert.Same(result, await pending);
    }
}
