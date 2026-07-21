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
    private const string ReadyStatus = "Ready";
    private const string ReviewStatus = "Review";
    private const string BlockedStatus = "Blocked";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] RequiredInstallEnvironmentVariables =
    [
        "SAFARSUITE_ENTITLEMENT_SIGNING_KEY_ID",
        "SAFARSUITE_ENTITLEMENT_SIGNING_SECRET"
    ];

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

    public string SigningKeyId => _activeKeyId;

    public ControlCloudBootstrapSecretReadiness GetSecretReadiness()
    {
        var warnings = new List<string>();
        var hasActiveSecret = _secretsByKeyId.TryGetValue(_activeKeyId, out var activeSecret)
            && !string.IsNullOrWhiteSpace(activeSecret);

        if (!hasActiveSecret)
        {
            warnings.Add("Active bootstrap/entitlement signing key has no configured secret.");

            return new ControlCloudBootstrapSecretReadiness(
                BlockedStatus,
                _activeKeyId,
                HasActiveSecret: false,
                warnings,
                RequiredInstallEnvironmentVariables,
                "Active signing key is missing usable secret material.");
        }

        if (LooksLikeDevelopmentKeyId(_activeKeyId))
        {
            warnings.Add("Active signing key id looks like local, development, or proof material.");
        }

        if (LooksLikePlaceholderSecret(activeSecret!))
        {
            warnings.Add("Active signing secret looks like placeholder or development material.");
        }

        var status = warnings.Count == 0 ? ReadyStatus : ReviewStatus;
        var detail = status == ReadyStatus
            ? "Active signing key is configured and does not match known development placeholders."
            : "Active signing configuration is usable, but should be reviewed before customer handoff.";

        return new ControlCloudBootstrapSecretReadiness(
            status,
            _activeKeyId,
            HasActiveSecret: true,
            warnings,
            RequiredInstallEnvironmentVariables,
            detail);
    }

    public ControlCloudSignedBootstrapPackage Sign(
        ControlCloudBootstrapPackagePayload payload)
    {
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);

        return new ControlCloudSignedBootstrapPackage(
            payloadJson,
            payload,
            SignPayloadJson(payloadJson));
    }

    public ControlCloudBootstrapPackageSignature SignPayloadJson(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            throw new ArgumentException("Payload JSON is required.", nameof(payloadJson));
        }

        var payloadSha256 = ComputeSha256(payloadJson);
        var signature = Sign(_secretsByKeyId[_activeKeyId], payloadJson);

        return new ControlCloudBootstrapPackageSignature(
            SignatureAlgorithm,
            _activeKeyId,
            payloadSha256,
            signature);
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

    private static bool LooksLikeDevelopmentKeyId(string keyId)
    {
        var normalized = keyId.Trim().ToLowerInvariant();

        return normalized.Contains("local", StringComparison.Ordinal)
            || normalized.Contains("dev", StringComparison.Ordinal)
            || normalized.Contains("proof", StringComparison.Ordinal)
            || normalized.Contains("compose", StringComparison.Ordinal);
    }

    private static bool LooksLikePlaceholderSecret(string secret)
    {
        var normalized = secret.Trim().ToLowerInvariant();

        return normalized.Length < 32
            || normalized.Contains("change-before", StringComparison.Ordinal)
            || normalized.Contains("change-me", StringComparison.Ordinal)
            || normalized.Contains("development", StringComparison.Ordinal)
            || normalized.Contains("local-entitlement-signing-secret", StringComparison.Ordinal);
    }
}
