using EasyPanel.Backend.Infrastructure.Entities;

namespace EasyPanel.Backend.Infrastructure.Security;

public interface IJwtTokenIssuer
{
    IssuedAccessToken IssueAccessToken(UserAccount user);
}

public sealed record IssuedAccessToken(string AccessToken, DateTimeOffset ExpiresAtUtc);
