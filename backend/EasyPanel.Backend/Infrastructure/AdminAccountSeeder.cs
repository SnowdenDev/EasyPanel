using System.Security.Cryptography;
using EasyPanel.Backend.Infrastructure.Entities;
using EasyPanel.Backend.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace EasyPanel.Backend.Infrastructure;

/// <summary>
/// If no users exist yet (a brand-new install), creates one Admin account with a random
/// password and prints it once. There is no other way to get the first login — write
/// this password down, it is never shown again.
/// </summary>
public static class AdminAccountSeeder
{
    public static async Task SeedIfNeededAsync(AppDbContext dbContext, IPasswordHasher passwordHasher, ILogger logger, CancellationToken cancellationToken)
    {
        if (await dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var generatedPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));

        var admin = new UserAccount
        {
            Id = Guid.NewGuid(),
            Email = "admin@localhost",
            PasswordHash = passwordHasher.HashPassword(generatedPassword),
            DisplayName = "Administrator",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        dbContext.Users.Add(admin);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "No users existed — created the first Admin account. Email: {Email} Password: {Password} (shown once, write it down now)",
            admin.Email,
            generatedPassword);
    }
}
