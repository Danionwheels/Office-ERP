using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Entitlements.Ports;
using SafarSuite.LocalServer.Domain.Entitlements;

namespace SafarSuite.LocalServer.Infrastructure.Entitlements;

public sealed class HmacLocalServerEntitlementBundleVerifier
    : ILocalServerEntitlementBundleVerifier
{
    private const string ExpectedSignatureAlgorithm = "HMAC-SHA256";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly LocalServerEntitlementTrustOptions _options;
    private readonly IReadOnlyDictionary<string, string> _secretsByKeyId;

    public HmacLocalServerEntitlementBundleVerifier(
        LocalServerEntitlementTrustOptions options)
    {
        _options = options;
        _secretsByKeyId = options.SigningKeys
            .Where(key => !string.IsNullOrWhiteSpace(key.KeyId) && !string.IsNullOrWhiteSpace(key.Secret))
            .GroupBy(key => key.KeyId.Trim(), StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.First().Secret.Trim(),
                StringComparer.Ordinal);
    }

    public LocalServerEntitlementBundleVerificationResult Verify(
        ClientPortalSignedEntitlementBundleResponse bundle,
        string expectedInstallationId,
        DateTimeOffset importedAtUtc)
    {
        var cleanInstallationId = expectedInstallationId.Trim();

        if (cleanInstallationId.Length == 0)
        {
            return Failure(
                "InstallationIdRequired",
                "Expected installation id is required.");
        }

        if (string.IsNullOrWhiteSpace(bundle.PayloadJson))
        {
            return Failure(
                "PayloadRequired",
                "Entitlement bundle payload JSON is required.");
        }

        if (bundle.Signature.Algorithm != ExpectedSignatureAlgorithm)
        {
            return Failure(
                "SignatureAlgorithmUnsupported",
                $"Signature algorithm '{bundle.Signature.Algorithm}' is not supported.");
        }

        if (!_secretsByKeyId.TryGetValue(bundle.Signature.KeyId.Trim(), out var signingSecret))
        {
            return Failure(
                "SigningKeyUnknown",
                $"Signing key '{bundle.Signature.KeyId}' is not trusted by this local server.");
        }

        var payloadSha256 = ComputeSha256(bundle.PayloadJson);

        if (!string.Equals(payloadSha256, bundle.Signature.PayloadSha256, StringComparison.Ordinal))
        {
            return Failure(
                "PayloadHashMismatch",
                "Entitlement bundle payload hash does not match the signed payload.");
        }

        var expectedSignature = Sign(signingSecret, bundle.PayloadJson);

        if (!FixedTimeEqualsBase64(expectedSignature, bundle.Signature.Value))
        {
            return Failure(
                "SignatureInvalid",
                "Entitlement bundle signature is not valid.");
        }

        ClientPortalEntitlementBundlePayloadResponse payload;

        try
        {
            payload = JsonSerializer.Deserialize<ClientPortalEntitlementBundlePayloadResponse>(
                bundle.PayloadJson,
                JsonOptions) ?? throw new JsonException("Payload JSON was empty.");
        }
        catch (JsonException exception)
        {
            return Failure(
                "PayloadInvalid",
                $"Entitlement bundle payload JSON could not be parsed: {exception.Message}");
        }

        if (!string.Equals(payload.InstallationId, cleanInstallationId, StringComparison.Ordinal))
        {
            return Failure(
                "InstallationMismatch",
                "Entitlement bundle belongs to a different installation.");
        }

        if (!string.Equals(payload.Issuer, _options.ExpectedIssuer, StringComparison.Ordinal))
        {
            return Failure(
                "IssuerMismatch",
                "Entitlement bundle issuer is not trusted.");
        }

        if (!string.Equals(payload.Audience, _options.ExpectedAudience, StringComparison.Ordinal))
        {
            return Failure(
                "AudienceMismatch",
                "Entitlement bundle audience is not valid for this local server.");
        }

        if (payload.GraceUntil < payload.PaidUntil
            || payload.OfflineValidUntil < payload.GraceUntil
            || payload.PaidUntil < payload.ValidFrom)
        {
            return Failure(
                "EntitlementDatesInvalid",
                "Entitlement bundle dates are not ordered correctly.");
        }

        var entitlement = new LocalServerCachedEntitlement(
            payload.BundleVersion,
            payload.Issuer,
            payload.Audience,
            payload.ClientId,
            payload.InstallationId,
            payload.EntitlementVersion,
            payload.BundleIssueId,
            payload.EntitlementSnapshotId,
            payload.ContractId,
            payload.SourceInvoiceId,
            payload.SourceInvoiceNumber,
            payload.Status,
            payload.BundleIssuedAtUtc,
            payload.EntitlementIssuedAtUtc,
            payload.ValidFrom,
            payload.PaidUntil,
            payload.WarningStartsAt,
            payload.GraceUntil,
            payload.OfflineValidUntil,
            payload.AllowedDevices,
            payload.AllowedBranches,
            payload.Modules
                .OrderBy(module => module.ModuleCode, StringComparer.Ordinal)
                .Select(module => new LocalServerEntitlementModule(
                    module.ModuleCode,
                    module.Status,
                    module.IsEnabled))
                .ToArray(),
            bundle.PayloadJson,
            bundle.Signature.Algorithm,
            bundle.Signature.KeyId,
            bundle.Signature.PayloadSha256,
            bundle.Signature.Value,
            importedAtUtc);

        return LocalServerEntitlementBundleVerificationResult.Success(entitlement);
    }

    private static LocalServerEntitlementBundleVerificationResult Failure(
        string failureCode,
        string detail)
    {
        return LocalServerEntitlementBundleVerificationResult.Failure(
            failureCode,
            detail);
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

    private static bool FixedTimeEqualsBase64(string expected, string actual)
    {
        try
        {
            var expectedBytes = Convert.FromBase64String(expected);
            var actualBytes = Convert.FromBase64String(actual);

            return expectedBytes.Length == actualBytes.Length
                && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
