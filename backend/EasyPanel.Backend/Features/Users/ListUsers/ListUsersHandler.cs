using EasyPanel.Backend.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Features.Users.ListUsers;

public sealed class ListUsersHandler(AppDbContext dbContext)
{
    public async Task<IReadOnlyList<UserSummary>> HandleAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .OrderBy(user => user.DisplayName)
            .Select(user => new UserSummary(
                user.Id,
                user.Email,
                user.DisplayName,
                user.Role,
                user.IsActive,
                user.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
