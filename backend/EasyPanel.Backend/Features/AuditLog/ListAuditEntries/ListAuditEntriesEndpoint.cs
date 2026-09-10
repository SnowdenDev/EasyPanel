using EasyPanel.Backend.Infrastructure.Entities;

namespace EasyPanel.Backend.Features.AuditLog.ListAuditEntries;

public static class ListAuditEntriesEndpoint
{
    public static void MapListAuditEntriesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/audit-log", async (
            Guid? instanceId,
            int? skip,
            int? take,
            ListAuditEntriesHandler handler,
            CancellationToken cancellationToken) =>
        {
            var entries = await handler.HandleAsync(instanceId, skip ?? 0, take ?? 50, cancellationToken);
            return Results.Ok(entries);
        })
        .WithName("ListAuditEntries")
        .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)));
    }
}
