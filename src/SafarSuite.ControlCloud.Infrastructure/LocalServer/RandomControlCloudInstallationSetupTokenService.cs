using System.Security.Cryptography;
using System.Text;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;

namespace SafarSuite.ControlCloud.Infrastructure.LocalServer;

public sealed class RandomControlCloudInstallationSetupTokenService
    : IControlCloudInstallationSetupTokenService
{
    private readonly ControlCloudSetupTokenOptions _options;

    public RandomControlCloudInstallationSetupTokenService(
        ControlCloudSetupTokenOptions options)
    {
        _options = options;
    }

    public string CreateSetupToken()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(
            Math.Clamp(_options.TokenBytes, 16, 64));

        return Base64UrlEncode(tokenBytes);
    }

    public string HashSecret(string secret)
    {
        var normalized = secret.Trim();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Base64UrlEncode(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
