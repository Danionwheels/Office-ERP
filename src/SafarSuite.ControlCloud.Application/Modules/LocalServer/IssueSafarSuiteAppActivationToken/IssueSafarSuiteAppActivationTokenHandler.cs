using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.IssueSafarSuiteAppActivationToken;

public sealed class IssueSafarSuiteAppActivationTokenHandler
{
    private const string ClaimsVersion = "safarsuite.activation.v1";
    private const string TokenUse = "LocalServerActivation";
    private const int TokenValidityDays = 7;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IControlCloudClientInstallationRepository _installations;
    private readonly IControlCloudEntitlementBundleIssueRepository _bundleIssues;
    private readonly IControlCloudAppActivationTokenSigner _signer;
    private readonly IControlCloudAppActivationIssueRepository _activationIssues;
    private readonly IClientPortalAuditRecorder _audit;
    private readonly IControlCloudClock _clock;

    public IssueSafarSuiteAppActivationTokenHandler(
        IControlCloudClientInstallationRepository installations,
        IControlCloudEntitlementBundleIssueRepository bundleIssues,
        IControlCloudAppActivationTokenSigner signer,
        IControlCloudAppActivationIssueRepository activationIssues,
        IClientPortalAuditRecorder audit,
        IControlCloudClock clock)
    {
        _installations = installations;
        _bundleIssues = bundleIssues;
        _signer = signer;
        _activationIssues = activationIssues;
        _audit = audit;
        _clock = clock;
    }

    public async Task<IssueSafarSuiteAppActivationTokenResult> HandleAsync(
        IssueSafarSuiteAppActivationTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        var installationId = NormalizeText(command.InstallationId);

        if (command.ClientId == Guid.Empty)
        {
            return IssueSafarSuiteAppActivationTokenResult.Failure(
                "ClientIdRequired",
                "Client id is required before issuing an app activation token.");
        }

        if (installationId is null)
        {
            return IssueSafarSuiteAppActivationTokenResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before issuing an app activation token.");
        }

        if (command.ServerInstallationId == Guid.Empty)
        {
            return IssueSafarSuiteAppActivationTokenResult.Failure(
                "AppServerInstallationIdRequired",
                "App server installation id is required before issuing an app activation token.");
        }

        var fingerprintHash = NormalizeText(command.FingerprintHash);
        var serverPublicKey = NormalizeText(command.ServerPublicKey);

        if (fingerprintHash is null)
        {
            return IssueSafarSuiteAppActivationTokenResult.Failure(
                "AppFingerprintRequired",
                "App activation fingerprint hash is required.");
        }

        if (serverPublicKey is null)
        {
            return IssueSafarSuiteAppActivationTokenResult.Failure(
                "AppServerPublicKeyRequired",
                "App server public key is required.");
        }

        var installation = await _installations.GetByInstallationIdAsync(
            installationId,
            cancellationToken);

        if (installation is null)
        {
            return IssueSafarSuiteAppActivationTokenResult.Failure(
                "InstallationNotRegistered",
                "Installation must be registered before an app activation token can be issued.");
        }

        if (installation.ClientId != command.ClientId)
        {
            return IssueSafarSuiteAppActivationTokenResult.Failure(
                "InstallationClientMismatch",
                "Installation id is already bound to another client.");
        }

        var replacesActivationIssueId = command.ReplacesActivationIssueId;

        if (replacesActivationIssueId == Guid.Empty)
        {
            return IssueSafarSuiteAppActivationTokenResult.Failure(
                "ActivationIssueReplacementIdInvalid",
                "Replacement activation issue id cannot be empty.");
        }

        if (replacesActivationIssueId.HasValue)
        {
            var replacedIssue = await _activationIssues.GetByIdAsync(
                replacesActivationIssueId.Value,
                cancellationToken);

            if (replacedIssue is null)
            {
                return IssueSafarSuiteAppActivationTokenResult.Failure(
                    "ActivationIssueReplacementNotFound",
                    "Replacement activation issue was not found.");
            }

            if (replacedIssue.ClientId != command.ClientId)
            {
                return IssueSafarSuiteAppActivationTokenResult.Failure(
                    "ActivationIssueReplacementClientMismatch",
                    "Replacement activation issue belongs to another client.");
            }

            if (!replacedIssue.InstallationId.Equals(installationId, StringComparison.Ordinal))
            {
                return IssueSafarSuiteAppActivationTokenResult.Failure(
                    "ActivationIssueReplacementInstallationMismatch",
                    "Replacement activation issue belongs to another provider installation.");
            }

            if (replacedIssue.Status != ControlCloudAppActivationIssueStatuses.Revoked)
            {
                return IssueSafarSuiteAppActivationTokenResult.Failure(
                    "ActivationIssueReplacementNotRevoked",
                    "Replacement activation issue must be revoked before a new app activation token can replace it.");
            }
        }

        var latestIssue = await _bundleIssues.GetLatestByInstallationIdAsync(
            installationId,
            cancellationToken);

        if (latestIssue is null)
        {
            return IssueSafarSuiteAppActivationTokenResult.Failure(
                "EntitlementNotFound",
                "No signed entitlement bundle has been issued for this installation.");
        }

        ControlCloudEntitlementBundlePayload payload;

        try
        {
            payload = JsonSerializer.Deserialize<ControlCloudEntitlementBundlePayload>(
                    latestIssue.PayloadJson,
                    JsonOptions)
                ?? throw new JsonException("Entitlement payload was empty.");
        }
        catch (JsonException exception)
        {
            return IssueSafarSuiteAppActivationTokenResult.Failure(
                "EntitlementPayloadInvalid",
                $"Latest entitlement payload could not be parsed: {exception.Message}");
        }

        if (payload.ClientId != command.ClientId
            || !payload.InstallationId.Equals(installationId, StringComparison.Ordinal))
        {
            return IssueSafarSuiteAppActivationTokenResult.Failure(
                "EntitlementInstallationMismatch",
                "Latest entitlement bundle is not bound to the requested client installation.");
        }

        var now = _clock.UtcNow;
        var activationRequestId = command.ActivationRequestId ?? Guid.NewGuid();
        var activationIssueId = Guid.NewGuid();
        var requestedBy = NormalizeText(command.RequestedBy) ?? "SafarSuite Control Cloud";
        var branchId = GuidFromStableText(installationId);
        var moduleEntitlements = BuildAppModuleEntitlements(payload.Modules);
        var claims = new SafarSuiteAppActivationTokenClaims(
            ClaimsVersion,
            TokenUse,
            activationRequestId,
            command.ServerInstallationId,
            fingerprintHash,
            serverPublicKey,
            command.ClientId,
            branchId,
            command.ClientId.ToString("N")[..12],
            "SafarSuite client",
            installation.DeploymentProfile.SiteId,
            payload.Status,
            payload.PaidUntil,
            payload.GraceUntil,
            payload.OfflineValidUntil,
            moduleEntitlements,
            _signer.SigningKeyId,
            now,
            now.AddMinutes(-5),
            now.AddDays(TokenValidityDays));
        var signed = _signer.Sign(claims);
        var response = new IssueSafarSuiteAppActivationTokenResponse(
            activationIssueId,
            command.ClientId,
            installationId,
            command.ServerInstallationId,
            activationRequestId,
            replacesActivationIssueId,
            payload.EntitlementVersion,
            _signer.SigningKeyId,
            now,
            claims.ExpiresAt,
            new SafarSuiteAppActivationTokenImportResponse(
                signed.ActivationToken,
                signed.Signature,
                _signer.SigningKeyId,
                command.ClientId,
                branchId,
                claims.CustomerCode,
                claims.CustomerName,
                claims.BranchName,
                payload.PaidUntil,
                payload.GraceUntil,
                payload.OfflineValidUntil,
                moduleEntitlements));

        await _activationIssues.AddAsync(
            ControlCloudAppActivationIssue.Create(
                response.ActivationIssueId,
                response.ClientId,
                response.InstallationId,
                response.AppServerInstallationId,
                response.ActivationRequestId,
                response.ReplacesActivationIssueId,
                fingerprintHash,
                Sha256Hex(serverPublicKey),
                response.EntitlementVersion,
                response.SigningKeyId,
                requestedBy,
                response.IssuedAtUtc,
                response.ExpiresAtUtc),
            cancellationToken);

        await ControlCloudAuditWriter.TryRecordAsync(
            _audit,
            new ClientPortalAuditRecord(
                Guid.NewGuid(),
                command.ClientId,
                InvitationId: null,
                UserId: null,
                SubjectEmail: "",
                ClientPortalAuditEventTypes.AppActivationTokenIssued,
                ControlCloudAuditWriter.NormalizeActor(requestedBy, ClientPortalAuditActors.ControlCloud),
                $"App activation token '{response.ActivationIssueId}' issued for installation '{installationId}', app server '{command.ServerInstallationId}', entitlement version '{payload.EntitlementVersion}', signing key '{_signer.SigningKeyId}', and replacement '{FormatNullableIssueId(replacesActivationIssueId)}'.",
                now),
            cancellationToken);

        return IssueSafarSuiteAppActivationTokenResult.Success(response);
    }

    private static IReadOnlyDictionary<string, bool> BuildAppModuleEntitlements(
        IReadOnlyCollection<ControlCloudEntitlementBundleModule> modules)
    {
        var entitlements = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            ["module.platform"] = true,
            ["module.identity-access"] = true,
            ["module.tenant-branch"] = true,
            ["module.module-registry"] = true,
            ["module.entitlements"] = true,
            ["module.notifications"] = true,
            ["module.audit"] = true,
            ["module.cloud-sync"] = false
        };

        foreach (var module in modules)
        {
            foreach (var appModuleCode in ToAppModuleCodes(module.ModuleCode))
            {
                entitlements[appModuleCode] = module.IsEnabled;
            }
        }

        return entitlements
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyCollection<string> ToAppModuleCodes(string moduleCode)
    {
        return moduleCode.Trim().ToUpperInvariant() switch
        {
            "ACCOUNTING" => ["module.accounting"],
            "REPORTS" or "REPORTING" or "REPORTINGCORE" or "REPORTING-CORE" => ["module.reporting-core"],
            "CLIENTS" or "PARTIES" or "CLIENTS-PARTIES" => ["module.clients-parties"],
            "TICKET" or "TICKETS" or "TICKET-STOCK" => ["module.ticket-stock"],
            "TRAVEL" => ["module.travel"],
            "TOUR" => ["module.tour"],
            "CLOUDSYNC" or "CLOUD-SYNC" => ["module.cloud-sync"],
            _ => []
        };
    }

    private static Guid GuidFromStableText(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));
        var bytes = hash.Take(16).ToArray();

        bytes[7] = (byte)((bytes[7] & 0x0F) | 0x40);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        return new Guid(bytes);
    }

    private static string Sha256Hex(string value)
    {
        return Convert
            .ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim())))
            .ToLowerInvariant();
    }

    private static string FormatNullableIssueId(Guid? activationIssueId)
    {
        return activationIssueId?.ToString("D") ?? "none";
    }

    private static string? NormalizeText(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
