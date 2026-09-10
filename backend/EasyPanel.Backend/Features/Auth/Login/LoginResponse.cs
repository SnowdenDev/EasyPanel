using EasyPanel.Backend.Infrastructure.Entities;

namespace EasyPanel.Backend.Features.Auth.Login;

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    Guid UserId,
    string DisplayName,
    UserRole Role
);
