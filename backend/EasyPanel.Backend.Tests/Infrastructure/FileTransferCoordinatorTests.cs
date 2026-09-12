using EasyPanel.Backend.Infrastructure;

namespace EasyPanel.Backend.Tests.Infrastructure;

public sealed class FileTransferCoordinatorTests
{
    [Fact]
    public async Task PushChunkAsync_RejectsDataFromAnotherNode()
    {
        var coordinator = new FileTransferCoordinator();
        var expectedNodeId = Guid.NewGuid();
        var transferId = Guid.NewGuid();
        var reader = coordinator.RegisterTransfer(expectedNodeId, transferId);

        var accepted = await coordinator.PushChunkAsync(
            Guid.NewGuid(),
            transferId,
            [1, 2, 3],
            isFinal: true,
            CancellationToken.None);

        Assert.False(accepted);
        Assert.False(reader.TryRead(out _));
    }

    [Fact]
    public async Task PushChunkAsync_AcceptsDataFromTheRegisteredNode()
    {
        var coordinator = new FileTransferCoordinator();
        var expectedNodeId = Guid.NewGuid();
        var transferId = Guid.NewGuid();
        var reader = coordinator.RegisterTransfer(expectedNodeId, transferId);

        var accepted = await coordinator.PushChunkAsync(
            expectedNodeId,
            transferId,
            [1, 2, 3],
            isFinal: true,
            CancellationToken.None);

        var chunk = Assert.IsType<FileTransferChunkEvent>(await reader.ReadAsync());
        Assert.True(accepted);
        Assert.Equal([1, 2, 3], chunk.Data);
        Assert.True(chunk.IsFinal);
    }
}
