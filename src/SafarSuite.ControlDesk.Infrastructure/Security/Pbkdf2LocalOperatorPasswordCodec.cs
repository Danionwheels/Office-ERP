using System.Globalization;
using System.Security.Cryptography;
using SafarSuite.ControlDesk.Application.Modules.Auth;

namespace SafarSuite.ControlDesk.Infrastructure.Security;

public sealed class Pbkdf2LocalOperatorPasswordCodec : ILocalOperatorPasswordCodec
{
    private const int Iterations = 120_000;
    private const int MinimumAcceptedIterations = 10_000;
    private const int MaximumAcceptedIterations = 1_000_000;
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const string Algorithm = "pbkdf2-sha256";

    public string Hash(string password)
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
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{Algorithm}.{Iterations}.{Base64UrlEncode(salt)}.{Base64UrlEncode(hash)}");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(salt);
            CryptographicOperations.ZeroMemory(hash);
        }
    }

    public bool Verify(string password, string? passwordHash)
    {
        ArgumentNullException.ThrowIfNull(password);

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        var parts = passwordHash.Split('.', 4);

        if (parts.Length != 4
            || parts[0] != Algorithm
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations)
            || iterations < MinimumAcceptedIterations
            || iterations > MaximumAcceptedIterations
            || iterations.ToString(CultureInfo.InvariantCulture) != parts[1])
        {
            return false;
        }

        byte[]? salt = null;
        byte[]? expectedHash = null;
        byte[]? actualHash = null;

        try
        {
            salt = Base64UrlDecode(parts[2]);
            expectedHash = Base64UrlDecode(parts[3]);

            if (salt.Length != SaltSizeBytes
                || expectedHash.Length != HashSizeBytes
                || Base64UrlEncode(salt) != parts[2]
                || Base64UrlEncode(expectedHash) != parts[3])
            {
                return false;
            }

            actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
        finally
        {
            Zero(salt);
            Zero(expectedHash);
            Zero(actualHash);
        }
    }

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var incoming = value.Replace('-', '+').Replace('_', '/');
        var padding = incoming.Length % 4;

        if (padding > 0)
        {
            incoming = incoming.PadRight(incoming.Length + 4 - padding, '=');
        }

        return Convert.FromBase64String(incoming);
    }

    private static void Zero(byte[]? value)
    {
        if (value is not null)
        {
            CryptographicOperations.ZeroMemory(value);
        }
    }
}
