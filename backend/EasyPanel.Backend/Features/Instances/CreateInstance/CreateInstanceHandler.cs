using System.Text.Json;
using EasyPanel.Backend.Infrastructure;
using EasyPanel.Backend.Infrastructure.Entities;
using EasyPanel.Contracts.Enums;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Features.Instances.CreateInstance;

public sealed record CreateInstanceResult(CreateInstanceResponse? Response, string? NodeNotFoundError);

public sealed class CreateInstanceHandler(AppDbContext dbContext)
{
    public async Task<CreateInstanceResult> HandleAsync(CreateInstanceRequest request, Guid actorUserId, CancellationToken cancellationToken)
    {
        var nodeExists = await dbContext.Nodes.AnyAsync(node => node.Id == request.NodeId, cancellationToken);
        if (!nodeExists)
        {
            return new CreateInstanceResult(null, $"No node with id '{request.NodeId}' exists.");
        }

        // The backend has no filesystem access to the node's disk (nodes are outbound-only)
        // — ExpectedExecutableSha256 is whatever the admin declared here. Fetching it from
        // the daemon instead is a Phase 2/3 convenience, not required for this to work.
        var instance = new Instance
        {
            Id = Guid.NewGuid(),
            NodeId = request.NodeId,
            DisplayName = request.DisplayName,
            WorkDirectory = request.WorkDirectory,
            ExecutableRelativePath = request.ExecutableRelativePath,
            ExpectedExecutableSha256 = request.ExpectedExecutableSha256.ToUpperInvariant(),
            LaunchArguments = request.LaunchArguments,
            EnvironmentVariablesJson = request.EnvironmentVariables is { Count: > 0 }
                ? JsonSerializer.Serialize(request.EnvironmentVariables)
                : null,
            Status = InstanceStatus.Stopped,
            AutoRestartEnabled = true,
            CpuLimitPercent = request.CpuLimitPercent,
            MemoryLimitMegabytes = request.MemoryLimitMegabytes,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };

        dbContext.Instances.Add(instance);

        dbContext.AuditLogEntries.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            InstanceId = instance.Id,
            NodeId = instance.NodeId,
            Action = "InstanceCreated",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateInstanceResult(new CreateInstanceResponse(instance.Id, instance.Status), null);
    }
}
