using System.Security.Cryptography;
using System.Text;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class ClientPortalMfaSecretProtector : IClientPortalMfaSecretProtector
{
    private const string Prefix = "client-portal-mfa-v1";
    private const int NonceBytes = 12;
    private const int TagBytes = 16;
    private static readonly byte[] AdditionalData =
        Encoding.UTF8.GetBytes("SafarSuite.ControlCloud.ClientPortal.Mfa.v1");
    private readonly byte[] _key;

    public ClientPortalMfaSecretProtector(ClientPortalAccessOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.MfaProtectionSecret))
        {
            throw new InvalidOperationException("Client Portal MFA protection is not configured.");
        }

        _key = SHA256.HashData(Encoding.UTF8.GetBytes(
            $"client-portal-mfa:{options.MfaProtectionSecret.Trim()}"));
    }

    public string Protect(string secret)
    {
        var plaintext = Encoding.UTF8.GetBytes(
            string.IsNullOrWhiteSpace(secret)
                ? throw new InvalidOperationException("A TOTP secret is required.")
                : secret.Trim());
        var nonce = RandomNumberGenerator.GetBytes(NonceBytes);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagBytes];

        using var aes = new AesGcm(_key, TagBytes);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, AdditionalData);

        return string.Join('.', Prefix, Encode(nonce), Encode(tag), Encode(ciphertext));
    }

    public bool TryUnprotect(string protectedSecret, out string secret)
    {
        secret = "";
        var parts = (protectedSecret ?? "").Split('.');

        if (parts.Length != 4 || !parts[0].Equals(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            var nonce = Decode(parts[1]);
            var tag = Decode(parts[2]);
            var ciphertext = Decode(parts[3]);
            var plaintext = new byte[ciphertext.Length];

            if (nonce.Length != NonceBytes || tag.Length != TagBytes)
            {
                return false;
            }

            using var aes = new AesGcm(_key, TagBytes);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, AdditionalData);
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

    private static string Encode(byte[] value) => Convert.ToBase64String(value)
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Decode(string value)
    {
        var text = value.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(text.PadRight(text.Length + ((4 - text.Length % 4) % 4), '='));
    }
}
