using System.Security.Cryptography;
using System.Text;
using SafarSuite.ControlCloud.Api.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Api.Modules.ProviderAccess;

public sealed class ProviderAccessTotpSecretProtector : IProviderAccessTotpSecretProtector
{
    public const string ProtectedPrefix = "pa-totp-v1";
    private static readonly byte[] AdditionalAuthenticatedData =
        Encoding.UTF8.GetBytes("SafarSuite.ControlCloud.ProviderAccess.TotpSecret.v1");
    private const int NonceByteCount = 12;
    private const int TagByteCount = 16;

    private readonly byte[] _key;

    public ProviderAccessTotpSecretProtector(ClientPortalProviderAccessOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.TotpProtectionSecret))
        {
            throw new InvalidOperationException(
                "ClientPortal:ProviderAccess:TotpProtectionSecret is required for provider TOTP secret protection.");
        }

        _key = SHA256.HashData(Encoding.UTF8.GetBytes(
            $"provider-access-totp-secret-protector:{options.TotpProtectionSecret.Trim()}"));
    }

    public string Protect(string secret)
    {
        var normalizedSecret = string.IsNullOrWhiteSpace(secret)
            ? throw new InvalidOperationException("Provider TOTP secret is required.")
            : secret.Trim();
        var plaintext = Encoding.UTF8.GetBytes(normalizedSecret);
        var ciphertext = new byte[plaintext.Length];
        var nonce = RandomNumberGenerator.GetBytes(NonceByteCount);
        var tag = new byte[TagByteCount];

        using var aes = new AesGcm(_key, TagByteCount);
        aes.Encrypt(
            nonce,
            plaintext,
            ciphertext,
            tag,
            AdditionalAuthenticatedData);

        return string.Join(
            ".",
            ProtectedPrefix,
            Base64UrlEncode(nonce),
            Base64UrlEncode(tag),
            Base64UrlEncode(ciphertext));
    }

    public bool TryUnprotect(
        string storedSecret,
        out string secret)
    {
        secret = "";

        if (string.IsNullOrWhiteSpace(storedSecret))
        {
            return false;
        }

        var trimmed = storedSecret.Trim();

        if (!IsProtected(trimmed))
        {
            secret = trimmed;
            return true;
        }

        var parts = trimmed.Split('.');

        if (parts.Length != 4)
        {
            return false;
        }

        try
        {
            var nonce = Base64UrlDecode(parts[1]);
            var tag = Base64UrlDecode(parts[2]);
            var ciphertext = Base64UrlDecode(parts[3]);
            var plaintext = new byte[ciphertext.Length];

            if (nonce.Length != NonceByteCount || tag.Length != TagByteCount)
            {
                return false;
            }

            using var aes = new AesGcm(_key, TagByteCount);
            aes.Decrypt(
                nonce,
                ciphertext,
                tag,
                plaintext,
                AdditionalAuthenticatedData);

            secret = Encoding.UTF8.GetString(plaintext);
            return !string.IsNullOrWhiteSpace(secret);
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public bool IsProtected(string storedSecret)
    {
        return !string.IsNullOrWhiteSpace(storedSecret)
            && storedSecret.Trim().StartsWith(
                $"{ProtectedPrefix}.",
                StringComparison.Ordinal);
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
