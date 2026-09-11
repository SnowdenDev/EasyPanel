using System.Text.Json;
using EasyPanel.Backend.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Features.Instances.GetInstanceStatus;

public sealed class GetInstanceStatusHandler(AppDbContext dbContext)
{
    public async Task<InstanceStatusDetails?> HandleAsync(Guid instanceId, CancellationToken cancellationToken)
    {
        var row = await dbContext.Instances
            .Where(instance => instance.Id == instanceId)
            .Select(instance => new
            {
                instance.Id,
                instance.DisplayName,
                instance.Status,
                instance.NodeId,
                IsNodeOnline = instance.Node!.IsOnline,
                instance.UpdatedAtUtc,
                instance.WorkDirectory,
                instance.ExecutableRelativePath,
                instance.ExpectedExecutableSha256,
                instance.LaunchArguments,
                instance.EnvironmentVariablesJson,
                instance.CpuLimitPercent,
                instance.MemoryLimitMegabytes,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var environmentVariables = row.EnvironmentVariablesJson is not null
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(row.EnvironmentVariablesJson)
            : null;

        return new InstanceStatusDetails(
            row.Id,
            row.DisplayName,
            row.Status,
            row.NodeId,
            row.IsNodeOnline,
            row.UpdatedAtUtc,
            row.WorkDirectory,
            row.ExecutableRelativePath,
            row.ExpectedExecutableSha256,
            row.LaunchArguments,
            environmentVariables,
            row.CpuLimitPercent,
            row.MemoryLimitMegabytes);
    }
}
