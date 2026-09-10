using EasyPanel.Backend.Infrastructure.Security;
using Xunit;

namespace EasyPanel.Backend.Tests.Infrastructure.Security;

public sealed class Argon2PasswordHasherTests
{
    private readonly Argon2PasswordHasher _hasher = new();

    [Fact]
    public void VerifyPassword_ReturnsTrue_ForTheCorrectPassword()
    {
        var encodedHash = _hasher.HashPassword("correct-password");

        Assert.True(_hasher.VerifyPassword("correct-password", encodedHash));
    }

    [Fact]
    public void VerifyPassword_ReturnsFalse_ForAWrongPassword()
    {
        var encodedHash = _hasher.HashPassword("correct-password");

        Assert.False(_hasher.VerifyPassword("wrong-password", encodedHash));
    }

    [Fact]
    public void HashPassword_ProducesADifferentEncodedHash_EachTime()
    {
        // Different random salts each call — this is what makes two admins with the same
        // password unable to tell that from their stored hashes.
        var firstHash = _hasher.HashPassword("same-password");
        var secondHash = _hasher.HashPassword("same-password");

        Assert.NotEqual(firstHash, secondHash);
        Assert.True(_hasher.VerifyPassword("same-password", firstHash));
        Assert.True(_hasher.VerifyPassword("same-password", secondHash));
    }

    [Theory]
    [InlineData("not-the-right-format")]
    [InlineData("argon2id$m=65536,t=3,p=2$onlyonepart")]
    [InlineData("bcrypt$somehash")]
    public void VerifyPassword_ReturnsFalse_ForAMalformedEncodedHash(string malformedEncodedHash)
    {
        Assert.False(_hasher.VerifyPassword("anything", malformedEncodedHash));
    }
}
