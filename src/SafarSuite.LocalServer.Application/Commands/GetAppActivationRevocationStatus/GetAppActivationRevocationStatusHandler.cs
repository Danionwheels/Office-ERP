using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Commands.Ports;
using SafarSuite.LocalServer.Application.Common;
using SafarSuite.LocalServer.Application.Registration.Ports;
using SafarSuite.LocalServer.Domain.Commands;

namespace SafarSuite.LocalServer.Application.Commands.GetAppActivationRevocationStatus;

public sealed class GetAppActivationRevocationStatusHandler
{
    private readonly ILocalServerAppActivationRevocationStore _revocations;
    private readonly ILocalServerBootstrapConfigurationStore _bootstrapConfigurations;
    private readonly ILocalServerClock _clock;

    public GetAppActivationRevocationStatusHandler(
        ILocalServerAppActivationRevocationStore revocations,
        ILocalServerBootstrapConfigurationStore bootstrapConfigurations,
        ILocalServerClock clock)
    {
        _revocations = revocations;
        _bootstrapConfigurations = bootstrapConfigurations;
        _clock = clock;
    }

    public async Task<GetAppActivationRevocationStatusResult> HandleAsync(
        GetAppActivationRevocationStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        var installationId = NormalizeRequiredText(query.InstallationId, 160);
        var fingerprintHash = NormalizeOptionalText(query.FingerprintHash, 512);
        var serverPublicKeySha256 = NormalizeOptionalText(query.ServerPublicKeySha256, 256);

        if (query.ClientId == Guid.Empty)
        {
            return GetAppActivationRevocationStatusResult.Failure(
                "ClientIdRequired",
                "Client id is required before checking app activation revocation status.");
        }

        if (installationId is null)
        {
            return GetAppActivationRevocationStatusResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before checking app activation revocation status.");
        }

        if (query.AppServerInstallationId == Guid.Empty)
        {
            return GetAppActivationRevocationStatusResult.Failure(
                "AppServerInstallationIdRequired",
                "App server installation id is required before checking app activation revocation status.");
        }

        if (query.ActivationIssueId == Guid.Empty)
        {
            return GetAppActivationRevocationStatusResult.Failure(
                "ActivationIssueIdRequired",
                "Activation issue id is required before checking app activation revocation status.");
        }

        var configuration = await _bootstrapConfigurations.GetCurrentAsync(cancellationToken);

        if (configuration is null)
        {
            return GetAppActivationRevocationStatusResult.Failure(
                "BootstrapConfigurationMissing",
                "A verified bootstrap configuration is required before checking app activation revocation status.");
        }

        if (configuration.ClientId != query.ClientId)
        {
            return GetAppActivationRevocationStatusResult.Failure(
                "AppActivationRevocationClientMismatch",
                "The requested client does not match this local-server bootstrap configuration.");
        }

        if (!string.Equals(configuration.InstallationId, installationId, StringComparison.Ordinal))
        {
            return GetAppActivationRevocationStatusResult.Failure(
                "AppActivationRevocationInstallationMismatch",
                "The requested installation does not match this local-server bootstrap configuration.");
        }

        var record = await _revocations.GetByActivationIssueIdAsync(
            query.ActivationIssueId,
            cancellationToken);

        if (record is null)
        {
            return GetAppActivationRevocationStatusResult.Success(
                new LocalServerAppActivationRevocationStatusResponse(
                    LocalServerAppActivationRevocationStatusFormat.Version,
                    query.ClientId,
                    installationId,
                    query.AppServerInstallationId,
                    query.ActivationIssueId,
                    IsRevoked: false,
                    IdentityMatched: true,
                    RevocationState: LocalServerAppActivationRevocationStates.NotRevoked,
                    Reason: "No local app activation revocation command has been recorded for this activation issue.",
                    CheckedAtUtc: _clock.UtcNow,
                    RevokedAtUtc: null,
                    RecordedAtUtc: null,
                    ActivationRequestId: null,
                    RevokedBy: null,
                    SigningKeyId: null,
                    CommandVersion: null));
        }

        var identityMatched = MatchesRequestIdentity(
            record,
            query.ClientId,
            installationId,
            query.AppServerInstallationId,
            fingerprintHash,
            serverPublicKeySha256);
        var revocationState = identityMatched
            ? LocalServerAppActivationRevocationStates.Revoked
            : LocalServerAppActivationRevocationStates.RevokedIdentityMismatch;
        var reason = identityMatched
            ? NormalizeOptionalText(record.Reason, 512) ?? "This app activation issue has been revoked locally."
            : "A local revocation exists for this activation issue, but the supplied app identity does not match the recorded issue. Treat the activation as blocked.";

        return GetAppActivationRevocationStatusResult.Success(
            new LocalServerAppActivationRevocationStatusResponse(
                LocalServerAppActivationRevocationStatusFormat.Version,
                query.ClientId,
                installationId,
                query.AppServerInstallationId,
                query.ActivationIssueId,
                IsRevoked: true,
                IdentityMatched: identityMatched,
                RevocationState: revocationState,
                Reason: reason,
                CheckedAtUtc: _clock.UtcNow,
                RevokedAtUtc: record.RevokedAtUtc,
                RecordedAtUtc: record.RecordedAtUtc,
                ActivationRequestId: record.ActivationRequestId,
                RevokedBy: record.RevokedBy,
                SigningKeyId: record.SigningKeyId,
                CommandVersion: record.CommandVersion));
    }

    private static bool MatchesRequestIdentity(
        LocalServerAppActivationRevocationRecord record,
        Guid clientId,
        string installationId,
        Guid appServerInstallationId,
        string? fingerprintHash,
        string? serverPublicKeySha256)
    {
        if (record.ClientId != clientId
            || !string.Equals(record.InstallationId, installationId, StringComparison.Ordinal)
            || record.AppServerInstallationId != appServerInstallationId)
        {
            return false;
        }

        if (fingerprintHash is not null
            && !string.Equals(record.FingerprintHash, fingerprintHash, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (serverPublicKeySha256 is not null
            && !string.Equals(record.ServerPublicKeySha256, serverPublicKeySha256, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static string? NormalizeRequiredText(string? value, int maxLength)
    {
        var normalized = NormalizeOptionalText(value, maxLength);

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
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
}
