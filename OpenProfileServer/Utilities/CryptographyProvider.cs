using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;

namespace OpenProfileServer.Utilities;

public static class CryptographyProvider
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 3;
    private const int MemorySize = 8192; // 8 MB
    private const int Parallelism = 2;

    /// <summary>
    /// Generates a secure hash and salt using Argon2id.
    /// </summary>
    /// <param name="password">The plain text password.</param>
    /// <returns>A tuple containing the Base64 encoded hash and salt.</returns>
    public static (string Hash, string Salt) CreateHash(string password)
    {
        var salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        var hashBytes = GenerateArgon2Hash(password, salt);

        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(salt));
    }

    /// <summary>
    /// Verifies a password against a stored hash and salt.
    /// </summary>
    public static bool Verify(string password, string storedHash, string storedSalt)
    {
        if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
            return false;

        try
        {
            var salt = Convert.FromBase64String(storedSalt);
            var expectedHash = Convert.FromBase64String(storedHash);

            var actualHash = GenerateArgon2Hash(password, salt);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] GenerateArgon2Hash(string password, byte[] salt)
    {
        var generator = new Argon2BytesGenerator();
        var parameters = new Argon2Parameters.Builder(Argon2Parameters.Argon2id)
            .WithVersion(Argon2Parameters.Version13)
            .WithIterations(Iterations)
            .WithMemoryAsKB(MemorySize)
            .WithParallelism(Parallelism)
            .WithSalt(salt)
            .Build();

        generator.Init(parameters);

        var hash = new byte[HashSize];
        generator.GenerateBytes(Encoding.UTF8.GetBytes(password), hash);

        return hash;
    }
}
