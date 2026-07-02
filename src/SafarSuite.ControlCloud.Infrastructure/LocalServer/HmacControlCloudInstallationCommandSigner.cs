using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;
using SafarSuite.ControlCloud.Infrastructure.ClientPortal;

namespace SafarSuite.ControlCloud.Infrastructure.LocalServer;

public sealed class HmacControlCloudInstallationCommandSigner
    : IControlCloudInstallationCommandSigner
{
    private const string SignatureAlgorithm = "HMAC-SHA256";
    private const string CommandSignatureVersion = "1";

    private readonly string _activeKeyId;
    private readonly IReadOnlyDictionary<string, string> _secretsByKeyId;

    public HmacControlCloudInstallationCommandSigner(
        ControlCloudEntitlementSigningOptions options)
    {
        _activeKeyId = options.ActiveKeyId.Trim();
        _secretsByKeyId = options.SigningKeys
            .Where(key => !string.IsNullOrWhiteSpace(key.KeyId) && !string.IsNullOrWhiteSpace(key.Secret))
            .GroupBy(key => key.KeyId.Trim(), StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.First().Secret.Trim(),
                StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(_activeKeyId))
        {
            throw new InvalidOperationException(
                "ControlCloud:EntitlementSigning:ActiveKeyId is required for command signing.");
        }

        if (!_secretsByKeyId.ContainsKey(_activeKeyId))
        {
            throw new InvalidOperationException(
                $"ControlCloud command signing key '{_activeKeyId}' is not configured.");
        }
    }

    public ControlCloudInstallationCommandSignature Sign(
        ControlCloudInstallationCommandSigningPayload payload)
    {
        var payloadSha256 = ComputeSha256(payload.PayloadJson);
        var signatureValue = Sign(
            _secretsByKeyId[_activeKeyId],
            BuildSignatureInput(payload, payloadSha256));

        return new ControlCloudInstallationCommandSignature(
            SignatureAlgorithm,
            _activeKeyId,
            payloadSha256,
            signatureValue);
    }

    private static string BuildSignatureInput(
        ControlCloudInstallationCommandSigningPayload payload,
        string payloadSha256)
    {
        return string.Join(
            "\n",
            CommandSignatureVersion,
            payload.CommandId.ToString("D", CultureInfo.InvariantCulture),
            payload.ClientId.ToString("D", CultureInfo.InvariantCulture),
            payload.InstallationId,
            payload.CommandVersion.ToString(CultureInfo.InvariantCulture),
            payload.CommandType,
            payload.QueuedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            payload.NotBeforeUtc?.ToString("O", CultureInfo.InvariantCulture) ?? "",
            payload.ExpiresAtUtc.ToString("O", CultureInfo.InvariantCulture),
            payloadSha256);
    }

    private static string ComputeSha256(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Sign(string signingSecret, string signatureInput)
    {
        var secretBytes = Encoding.UTF8.GetBytes(signingSecret);
        var inputBytes = Encoding.UTF8.GetBytes(signatureInput);
        var signatureBytes = HMACSHA256.HashData(secretBytes, inputBytes);

        return Convert.ToBase64String(signatureBytes);
    }
}
