using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Registration.Ports;
using SafarSuite.LocalServer.Domain.Registration;

namespace SafarSuite.LocalServer.Infrastructure.Registration;

public sealed class HmacLocalServerBootstrapBundleVerifier
    : ILocalServerBootstrapBundleVerifier
{
    private const string ExpectedSignatureAlgorithm = "HMAC-SHA256";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IReadOnlyDictionary<string, string> _secretsByKeyId;

    public HmacLocalServerBootstrapBundleVerifier(
        LocalServerBootstrapTrustOptions options)
    {
        _secretsByKeyId = options.SigningKeys
            .Where(key => !string.IsNullOrWhiteSpace(key.KeyId) && !string.IsNullOrWhiteSpace(key.Secret))
            .GroupBy(key => key.KeyId.Trim(), StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.First().Secret.Trim(),
                StringComparer.Ordinal);
    }

    public LocalServerBootstrapBundleVerificationResult Verify(
        LocalServerSignedBootstrapBundleResponse bundle,
        DateTimeOffset importedAtUtc,
        string? expectedInstallationId = null)
    {
        if (string.IsNullOrWhiteSpace(bundle.PayloadJson))
        {
            return Failure(
                "PayloadRequired",
                "Bootstrap bundle payload JSON is required.");
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
                "Bootstrap bundle payload hash does not match the signed payload.");
        }

        var expectedSignature = Sign(signingSecret, bundle.PayloadJson);

        if (!FixedTimeEqualsBase64(expectedSignature, bundle.Signature.Value))
        {
            return Failure(
                "SignatureInvalid",
                "Bootstrap bundle signature is not valid.");
        }

        LocalServerBootstrapPackagePayloadResponse payload;

        try
        {
            payload = JsonSerializer.Deserialize<LocalServerBootstrapPackagePayloadResponse>(
                bundle.PayloadJson,
                JsonOptions) ?? throw new JsonException("Payload JSON was empty.");
        }
        catch (JsonException exception)
        {
            return Failure(
                "PayloadInvalid",
                $"Bootstrap bundle payload JSON could not be parsed: {exception.Message}");
        }

        return ValidateAndCreateConfiguration(
            payload,
            bundle,
            importedAtUtc,
            expectedInstallationId);
    }

    private static LocalServerBootstrapBundleVerificationResult ValidateAndCreateConfiguration(
        LocalServerBootstrapPackagePayloadResponse payload,
        LocalServerSignedBootstrapBundleResponse bundle,
        DateTimeOffset importedAtUtc,
        string? expectedInstallationId)
    {
        if (!string.Equals(
                payload.FormatVersion,
                ControlCloudLocalServerBootstrapPackageFormat.Version,
                StringComparison.Ordinal))
        {
            return Failure(
                "FormatVersionUnsupported",
                "Bootstrap bundle format version is not supported.");
        }

        if (payload.ClientId == Guid.Empty)
        {
            return Failure(
                "ClientIdRequired",
                "Bootstrap bundle client id is required.");
        }

        var installationId = NormalizeRequiredText(payload.InstallationId);

        if (installationId is null)
        {
            return Failure(
                "InstallationIdRequired",
                "Bootstrap bundle installation id is required.");
        }

        var cleanExpectedInstallationId = NormalizeOptionalText(expectedInstallationId, 160);

        if (cleanExpectedInstallationId is not null
            && !string.Equals(installationId, cleanExpectedInstallationId, StringComparison.Ordinal))
        {
            return Failure(
                "InstallationMismatch",
                "Bootstrap bundle belongs to a different installation.");
        }

        if (payload.SetupTokenExpiresAtUtc <= importedAtUtc)
        {
            return Failure(
                "SetupTokenExpired",
                "Bootstrap bundle setup token has expired.");
        }

        if (NormalizeRequiredText(payload.SetupToken) is null)
        {
            return Failure(
                "SetupTokenRequired",
                "Bootstrap bundle setup token is required.");
        }

        if (!ControlCloudBootstrapModes.IsSupported(payload.DeploymentMode))
        {
            return Failure(
                "BootstrapModeUnsupported",
                "Bootstrap bundle deployment mode is not supported.");
        }

        if (!SafarSuiteClientDeploymentModes.IsSupported(payload.DeploymentProfile.ClientDeploymentMode))
        {
            return Failure(
                "ClientDeploymentModeUnsupported",
                "Bootstrap bundle client deployment mode is not supported.");
        }

        if (!SafarSuiteDeploymentSiteRoles.IsSupported(payload.DeploymentProfile.SiteRole))
        {
            return Failure(
                "SiteRoleUnsupported",
                "Bootstrap bundle site role is not supported.");
        }

        var cloudBaseUrl = NormalizeAbsoluteHttpUrl(payload.CloudBaseUrl);

        if (cloudBaseUrl is null)
        {
            return Failure(
                "CloudBaseUrlInvalid",
                "Bootstrap bundle cloud base URL must be an absolute HTTP or HTTPS URL.");
        }

        var endpoints = ValidateEndpoints(payload.Endpoints);

        if (endpoints.Failure is not null)
        {
            return endpoints.Failure;
        }

        var runtimePlan = payload.RuntimePlan is null
            ? null
            : new LocalServerBootstrapRuntimePlan(
                NormalizeRequiredText(payload.RuntimePlan.RuntimeMode) ?? "Unknown",
                NormalizeRequiredText(payload.RuntimePlan.ComposeProjectName) ?? "safarsuite-local-server",
                NormalizeRequiredText(payload.RuntimePlan.ConfigDirectory) ?? "",
                NormalizeRequiredText(payload.RuntimePlan.StateDirectory) ?? "",
                NormalizeRequiredText(payload.RuntimePlan.LocalServerVersion) ?? "Unknown",
                NormalizeRequiredText(payload.RuntimePlan.SafarSuiteAppVersion)
                    ?? NormalizeRequiredText(payload.LocalServerVersion)
                    ?? "Unknown",
                payload.RuntimePlan.Services
                    .Select(service => new LocalServerBootstrapRuntimeService(
                        NormalizeRequiredText(service.ServiceName) ?? "unknown",
                        NormalizeRequiredText(service.ServiceRole) ?? "",
                        service.StartsByDefault,
                        NormalizeOptionalText(service.ComposeProfile, 80),
                        NormalizeRequiredText(service.ImageEnvironmentVariable) ?? "",
                        NormalizeOptionalText(service.PublishedPortEnvironmentVariable, 120),
                        NormalizeRequiredText(service.InternalBaseUrl) ?? "",
                        NormalizeRequiredText(service.HealthUrl) ?? "",
                        service.DependsOn
                            .Where(dependency => !string.IsNullOrWhiteSpace(dependency))
                            .Select(dependency => dependency.Trim())
                            .ToArray()))
                    .ToArray());

        var configuration = new LocalServerBootstrapConfiguration(
            payload.FormatVersion,
            payload.BootstrapPackageId,
            payload.SetupTokenId,
            payload.ClientId,
            installationId,
            ControlCloudBootstrapModes.NormalizeOrDefault(payload.DeploymentMode),
            new LocalServerBootstrapDeploymentProfile(
                ControlCloudBootstrapModes.NormalizeOrDefault(payload.DeploymentProfile.BootstrapMode),
                SafarSuiteClientDeploymentModes.NormalizeOrDefault(payload.DeploymentProfile.ClientDeploymentMode),
                NormalizeRequiredText(payload.DeploymentProfile.SiteId) ?? installationId,
                SafarSuiteDeploymentSiteRoles.NormalizeOrDefault(
                    payload.DeploymentProfile.SiteRole,
                    payload.DeploymentProfile.ClientDeploymentMode),
                NormalizeOptionalText(payload.DeploymentProfile.ParentSiteId, 96),
                NormalizeOptionalText(payload.DeploymentProfile.BranchCode, 64),
                NormalizeOptionalText(payload.DeploymentProfile.SyncTopologyId, 96)),
            cloudBaseUrl,
            NormalizeRequiredText(payload.LocalServerVersion) ?? "Unknown",
            payload.SetupToken.Trim(),
            payload.SetupTokenExpiresAtUtc,
            payload.GeneratedAtUtc,
            endpoints.Value!,
            runtimePlan,
            bundle.PayloadJson,
            bundle.Signature.Algorithm,
            bundle.Signature.KeyId.Trim(),
            bundle.Signature.PayloadSha256,
            bundle.Signature.Value,
            importedAtUtc,
            LocalServerBootstrapRegistrationStatuses.Imported);

        return LocalServerBootstrapBundleVerificationResult.Success(configuration);
    }

    private static (LocalServerBootstrapEndpoints? Value, LocalServerBootstrapBundleVerificationResult? Failure)
        ValidateEndpoints(LocalServerBootstrapPackageEndpointsResponse endpoints)
    {
        var registrationUrl = NormalizeAbsoluteHttpUrl(endpoints.RegistrationUrl);
        var entitlementBundleUrl = NormalizeAbsoluteHttpUrl(endpoints.EntitlementBundleUrl);
        var heartbeatUrl = NormalizeAbsoluteHttpUrl(endpoints.HeartbeatUrl);
        var pendingCommandsUrl = NormalizeAbsoluteHttpUrl(endpoints.PendingCommandsUrl);
        var diagnosticsUrl = NormalizeAbsoluteHttpUrl(endpoints.DiagnosticsUrl);

        if (registrationUrl is null)
        {
            return (null, Failure(
                "RegistrationUrlInvalid",
                "Bootstrap bundle registration URL must be an absolute HTTP or HTTPS URL."));
        }

        if (entitlementBundleUrl is null)
        {
            return (null, Failure(
                "EntitlementBundleUrlInvalid",
                "Bootstrap bundle entitlement bundle URL must be an absolute HTTP or HTTPS URL."));
        }

        if (heartbeatUrl is null)
        {
            return (null, Failure(
                "HeartbeatUrlInvalid",
                "Bootstrap bundle heartbeat URL must be an absolute HTTP or HTTPS URL."));
        }

        if (pendingCommandsUrl is null)
        {
            return (null, Failure(
                "PendingCommandsUrlInvalid",
                "Bootstrap bundle pending commands URL must be an absolute HTTP or HTTPS URL."));
        }

        if (!string.IsNullOrWhiteSpace(endpoints.DiagnosticsUrl)
            && diagnosticsUrl is null)
        {
            return (null, Failure(
                "DiagnosticsUrlInvalid",
                "Bootstrap bundle diagnostics URL must be an absolute HTTP or HTTPS URL when provided."));
        }

        return (
            new LocalServerBootstrapEndpoints(
                registrationUrl,
                entitlementBundleUrl,
                heartbeatUrl,
                pendingCommandsUrl,
                diagnosticsUrl),
            null);
    }

    private static LocalServerBootstrapBundleVerificationResult Failure(
        string failureCode,
        string detail)
    {
        return LocalServerBootstrapBundleVerificationResult.Failure(
            failureCode,
            detail);
    }

    private static string? NormalizeRequiredText(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }

    private static string? NormalizeAbsoluteHttpUrl(string? value)
    {
        var normalized = NormalizeRequiredText(value)?.TrimEnd('/');

        return Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            && (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            ? normalized
            : null;
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
