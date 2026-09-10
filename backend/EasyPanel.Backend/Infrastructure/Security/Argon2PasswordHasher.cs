using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace EasyPanel.Backend.Infrastructure.Security;

/// <summary>
/// Argon2id password hashing. The encoded hash embeds its own work factors
/// (memory/iterations/parallelism) so they can be tuned later without breaking
/// verification of passwords hashed under the old factors.
/// </summary>
public sealed class Argon2PasswordHasher : IPasswordHasher
{
    private const int SaltSizeInBytes = 16;
    private const int HashSizeInBytes = 32;
    private const int MemorySizeInKilobytes = 65536;
    private const int Iterations = 3;
    private const int DegreeOfParallelism = 2;

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeInBytes);
        var hash = ComputeHash(password, salt, MemorySizeInKilobytes, Iterations, DegreeOfParallelism);

        return string.Join(
            '$',
            "argon2id",
            $"m={MemorySizeInKilobytes},t={Iterations},p={DegreeOfParallelism}",
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool VerifyPassword(string password, string encodedHash)
    {
        var parts = encodedHash.Split('$');
        if (parts.Length != 4 || parts[0] != "argon2id")
        {
            return false;
        }

        var parameters = ParseWorkFactors(parts[1]);
        if (parameters is null)
        {
            return false;
        }

        var (memorySizeInKilobytes, iterations, degreeOfParallelism) = parameters.Value;
        var salt = Convert.FromBase64String(parts[2]);
        var expectedHash = Convert.FromBase64String(parts[3]);

        var actualHash = ComputeHash(password, salt, memorySizeInKilobytes, iterations, degreeOfParallelism);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private static byte[] ComputeHash(string password, byte[] salt, int memorySizeInKilobytes, int iterations, int degreeOfParallelism)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memorySizeInKilobytes,
            Iterations = iterations,
            DegreeOfParallelism = degreeOfParallelism,
        };

        return argon2.GetBytes(HashSizeInBytes);
    }

    private static (int MemorySizeInKilobytes, int Iterations, int DegreeOfParallelism)? ParseWorkFactors(string encoded)
    {
        // encoded looks like "m=65536,t=3,p=2"
        var values = new Dictionary<char, int>();
        foreach (var pair in encoded.Split(','))
        {
            var keyValue = pair.Split('=');
            if (keyValue.Length != 2 || keyValue[0].Length != 1 || !int.TryParse(keyValue[1], out var value))
            {
                return null;
            }

            values[keyValue[0][0]] = value;
        }

        if (!values.TryGetValue('m', out var memory) || !values.TryGetValue('t', out var time) || !values.TryGetValue('p', out var parallelism))
        {
            return null;
        }

        return (memory, time, parallelism);
    }
}
