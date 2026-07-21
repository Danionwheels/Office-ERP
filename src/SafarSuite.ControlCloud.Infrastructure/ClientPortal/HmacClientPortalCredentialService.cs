using System.Security.Cryptography;
using System.Text;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class HmacClientPortalCredentialService : IClientPortalCredentialService
{
    private const int PasswordSaltBytes = 16;
    private const int PasswordHashBytes = 32;
    private const int PasswordIterations = 120_000;

    private readonly ClientPortalAccessOptions _options;

    public HmacClientPortalCredentialService(ClientPortalAccessOptions options)
    {
        _options = options;
    }

    public string CreateInvitationToken()
    {
        return CreateSecureToken(_options.InvitationTokenBytes);
    }

    public string CreateSecureToken(int byteCount = 32)
    {
        return Base64UrlEncode(RandomNumberGenerator.GetBytes(
            Math.Clamp(byteCount, 32, 64)));
    }

    public IReadOnlyCollection<string> CreateRecoveryCodes(int count = 10)
    {
        return Enumerable.Range(0, Math.Clamp(count, 1, 20))
            .Select(_ =>
            {
                var text = Convert.ToHexString(RandomNumberGenerator.GetBytes(10));
                return string.Join('-', text[..5], text[5..10], text[10..15], text[15..20]);
            })
            .ToArray();
    }

    public string NormalizeRecoveryCode(string recoveryCode)
    {
        return new string((recoveryCode ?? "")
            .Where(character => !char.IsWhiteSpace(character) && character != '-')
            .Select(char.ToUpperInvariant)
            .ToArray());
    }

    public string HashSecret(string secret)
    {
        using var hmac = new HMACSHA256(
            Encoding.UTF8.GetBytes(_options.SessionSigningSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(secret.Trim()));

        return Base64UrlEncode(hash);
    }

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(PasswordSaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            PasswordIterations,
            HashAlgorithmName.SHA256,
            PasswordHashBytes);

        return string.Join(
            ".",
            "pbkdf2-sha256",
            PasswordIterations.ToString(),
            Base64UrlEncode(salt),
            Base64UrlEncode(hash));
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        var parts = passwordHash.Split('.', 4);

        if (parts.Length != 4 || parts[0] != "pbkdf2-sha256")
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        var salt = Base64UrlDecode(parts[2]);
        var expectedHash = Base64UrlDecode(parts[3]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private static string Base64UrlEncode(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var incoming = value
            .Replace('-', '+')
            .Replace('_', '/');
        var padding = incoming.Length % 4;

        if (padding > 0)
        {
            incoming = incoming.PadRight(incoming.Length + 4 - padding, '=');
        }

        return Convert.FromBase64String(incoming);
    }
}
