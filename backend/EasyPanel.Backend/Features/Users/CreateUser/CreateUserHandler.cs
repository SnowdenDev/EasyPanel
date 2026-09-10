using EasyPanel.Backend.Infrastructure;
using EasyPanel.Backend.Infrastructure.Entities;
using EasyPanel.Backend.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Features.Users.CreateUser;

public sealed class CreateUserHandler(AppDbContext dbContext, IPasswordHasher passwordHasher)
{
    public async Task<CreateUserResult> HandleAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var emailAlreadyInUse = await dbContext.Users.AnyAsync(user => user.Email == request.Email, cancellationToken);
        if (emailAlreadyInUse)
        {
            return new CreateUserResult(null, $"A user with email '{request.Email}' already exists.");
        }

        var user = new UserAccount
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            DisplayName = request.DisplayName,
            Role = request.Role,
            PasswordHash = passwordHasher.HashPassword(request.Password),
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateUserResult(user.Id, null);
    }
}
