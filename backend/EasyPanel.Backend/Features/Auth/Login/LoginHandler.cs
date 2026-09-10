using EasyPanel.Backend.Infrastructure;
using EasyPanel.Backend.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Features.Auth.Login;

public sealed class LoginHandler(AppDbContext dbContext, IPasswordHasher passwordHasher, IJwtTokenIssuer jwtTokenIssuer)
{
    public async Task<LoginResponse?> HandleAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .SingleOrDefaultAsync(candidate => candidate.Email == request.Email, cancellationToken);

        if (user is null || !user.IsActive || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return null;
        }

        var issuedToken = jwtTokenIssuer.IssueAccessToken(user);

        return new LoginResponse(issuedToken.AccessToken, issuedToken.ExpiresAtUtc, user.Id, user.DisplayName, user.Role);
    }
}
