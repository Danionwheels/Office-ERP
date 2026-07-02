using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class HmacControlCloudEntitlementBundleSigner : IControlCloudEntitlementBundleSigner
{
    private const string SignatureAlgorithm = "HMAC-SHA256";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _activeKeyId;
    private readonly IReadOnlyDictionary<string, string> _secretsByKeyId;

    public HmacControlCloudEntitlementBundleSigner(ControlCloudEntitlementSigningOptions options)
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
                "ControlCloud:EntitlementSigning:ActiveKeyId is required.");
        }

        if (!_secretsByKeyId.ContainsKey(_activeKeyId))
        {
            throw new InvalidOperationException(
                $"ControlCloud entitlement signing key '{_activeKeyId}' is not configured.");
        }
    }

    public ControlCloudSignedEntitlementBundle Sign(ControlCloudEntitlementBundlePayload payload)
    {
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
        var payloadSha256 = ComputeSha256(payloadJson);
        var signature = Sign(_secretsByKeyId[_activeKeyId], payloadJson);

        return new ControlCloudSignedEntitlementBundle(
            payloadJson,
            payload,
            new ControlCloudEntitlementBundleSignature(
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

    private static string Sign(string signingSecret, string payloadJson)
    {
        var secretBytes = Encoding.UTF8.GetBytes(signingSecret);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        var signatureBytes = HMACSHA256.HashData(secretBytes, payloadBytes);

        return Convert.ToBase64String(signatureBytes);
    }
}
