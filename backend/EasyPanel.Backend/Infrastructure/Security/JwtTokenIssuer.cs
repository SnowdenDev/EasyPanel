using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EasyPanel.Backend.Infrastructure.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EasyPanel.Backend.Infrastructure.Security;

public sealed class JwtTokenIssuer(IOptions<JwtOptions> jwtOptions) : IJwtTokenIssuer
{
    private readonly JwtOptions _options = jwtOptions.Value;

    public IssuedAccessToken IssueAccessToken(UserAccount user)
    {
        var expiresAtUtc = DateTimeOffset.UtcNow.Add(_options.AccessTokenLifetime);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
        };

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: signingCredentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new IssuedAccessToken(accessToken, expiresAtUtc);
    }
}
