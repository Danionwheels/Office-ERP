using System.Security.Cryptography;

namespace SafarSuite.StagingPreflight;

public static class OperatorPasswordHasher
{
    public const int Iterations = 120_000;
    public const int SaltSizeBytes = 16;
    public const int HashSizeBytes = 32;

    public static string HashPassword(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSizeBytes);

        try
        {
            return $"pbkdf2-sha256.{Iterations}.{Base64UrlEncode(salt)}.{Base64UrlEncode(hash)}";
        }
        finally
        {
            CryptographicOperations.ZeroMemory(salt);
            CryptographicOperations.ZeroMemory(hash);
        }
    }

    public static bool VerifyPassword(string password, string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(passwordHash);

        if (!CryptographicMaterialValidator.IsValidOperatorPasswordHash(passwordHash))
        {
            return false;
        }

        var parts = passwordHash.Split('.', StringSplitOptions.None);
        var iterations = int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
        var salt = Base64UrlDecode(parts[2]);
        var expected = Base64UrlDecode(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expected.Length);

        try
        {
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(salt);
            CryptographicOperations.ZeroMemory(expected);
            CryptographicOperations.ZeroMemory(actual);
        }
    }

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        var remainder = base64.Length % 4;

        if (remainder > 0)
        {
            base64 = base64.PadRight(base64.Length + 4 - remainder, '=');
        }

        return Convert.FromBase64String(base64);
    }
}
