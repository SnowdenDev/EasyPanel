using System.Text.Json;
using EasyPanel.Backend.Infrastructure;
using EasyPanel.Backend.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Features.Instances.UpdateInstance;

public sealed class UpdateInstanceHandler(AppDbContext dbContext)
{
    public async Task<UpdateInstanceResult> HandleAsync(Guid instanceId, UpdateInstanceRequest request, Guid actorUserId, CancellationToken cancellationToken)
    {
        var instance = await dbContext.Instances.SingleOrDefaultAsync(candidate => candidate.Id == instanceId, cancellationToken);
        if (instance is null)
        {
            return new UpdateInstanceResult(false, $"No instance with id '{instanceId}' exists.");
        }

        // Edits only ever change what the *next* launch uses — there's no live
        // reconfiguration of an already-running process, same as every other panel like
        // this. Deliberately not blocking edits while Running/Starting: an admin fixing a
        // wrong hash or path doesn't need to stop the instance first just to correct it.
        instance.DisplayName = request.DisplayName;
        instance.WorkDirectory = request.WorkDirectory;
        instance.ExecutableRelativePath = request.ExecutableRelativePath;
        instance.ExpectedExecutableSha256 = request.ExpectedExecutableSha256.ToUpperInvariant();
        instance.LaunchArguments = request.LaunchArguments;
        instance.EnvironmentVariablesJson = request.EnvironmentVariables is { Count: > 0 }
            ? JsonSerializer.Serialize(request.EnvironmentVariables)
            : null;
        instance.CpuLimitPercent = request.CpuLimitPercent;
        instance.MemoryLimitMegabytes = request.MemoryLimitMegabytes;
        instance.UpdatedAtUtc = DateTimeOffset.UtcNow;

        dbContext.AuditLogEntries.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            InstanceId = instance.Id,
            NodeId = instance.NodeId,
            Action = "InstanceUpdated",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateInstanceResult(true, null);
    }
}
