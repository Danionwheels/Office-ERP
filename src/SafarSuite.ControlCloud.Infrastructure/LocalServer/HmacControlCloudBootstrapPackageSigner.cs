using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;
using SafarSuite.ControlCloud.Infrastructure.ClientPortal;

namespace SafarSuite.ControlCloud.Infrastructure.LocalServer;

public sealed class HmacControlCloudBootstrapPackageSigner
    : IControlCloudBootstrapPackageSigner
{
    private const string SignatureAlgorithm = "HMAC-SHA256";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _activeKeyId;
    private readonly IReadOnlyDictionary<string, string> _secretsByKeyId;

    public HmacControlCloudBootstrapPackageSigner(
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
                "ControlCloud:EntitlementSigning:ActiveKeyId is required for bootstrap package signing.");
        }

        if (!_secretsByKeyId.ContainsKey(_activeKeyId))
        {
            throw new InvalidOperationException(
                $"Control Cloud bootstrap package signing key '{_activeKeyId}' is not configured.");
        }
    }

    public ControlCloudSignedBootstrapPackage Sign(
        ControlCloudBootstrapPackagePayload payload)
    {
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
        var payloadSha256 = ComputeSha256(payloadJson);
        var signature = Sign(_secretsByKeyId[_activeKeyId], payloadJson);

        return new ControlCloudSignedBootstrapPackage(
            payloadJson,
            payload,
            new ControlCloudBootstrapPackageSignature(
                SignatureAlgorithm,
                _activeKeyId,
                payloadSha256,
                signature));
    }

    private static string ComputeSha256(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Sign(
        string signingSecret,
        string payloadJson)
    {
        var secretBytes = Encoding.UTF8.GetBytes(signingSecret);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        var signatureBytes = HMACSHA256.HashData(secretBytes, payloadBytes);

        return Convert.ToBase64String(signatureBytes);
    }
}
