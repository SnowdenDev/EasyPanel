using EasyPanel.Backend.Infrastructure.Entities;

namespace EasyPanel.Backend.Features.Users.ListUsers;

public sealed record UserSummary(
    Guid Id,
    string Email,
    string DisplayName,
    UserRole Role,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);
