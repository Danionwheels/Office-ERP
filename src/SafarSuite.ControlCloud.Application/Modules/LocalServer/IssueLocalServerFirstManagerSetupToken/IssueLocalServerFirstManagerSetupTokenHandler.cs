using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.IssueLocalServerFirstManagerSetupToken;

public sealed class IssueLocalServerFirstManagerSetupTokenHandler
{
    private readonly IControlCloudClientInstallationRepository _installations;
    private readonly IControlCloudFirstManagerSetupTokenSigner _signer;
    private readonly IControlCloudFirstManagerSetupTokenIssueRepository _issues;
    private readonly IClientPortalAuditRecorder _audit;
    private readonly IControlCloudClock _clock;

    public IssueLocalServerFirstManagerSetupTokenHandler(
        IControlCloudClientInstallationRepository installations,
        IControlCloudFirstManagerSetupTokenSigner signer,
        IControlCloudFirstManagerSetupTokenIssueRepository issues,
        IClientPortalAuditRecorder audit,
        IControlCloudClock clock)
    {
        _installations = installations;
        _signer = signer;
        _issues = issues;
        _audit = audit;
        _clock = clock;
    }

    public async Task<IssueLocalServerFirstManagerSetupTokenResult> HandleAsync(
        IssueLocalServerFirstManagerSetupTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        var installationId = NormalizeText(command.InstallationId);

        if (command.ClientId == Guid.Empty)
        {
            return IssueLocalServerFirstManagerSetupTokenResult.Failure(
                "ClientIdRequired",
                "Client id is required before issuing a first-manager setup token.");
        }

        if (installationId is null)
        {
            return IssueLocalServerFirstManagerSetupTokenResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before issuing a first-manager setup token.");
        }

        if (command.PendingDeviceRequestId == Guid.Empty)
        {
            return IssueLocalServerFirstManagerSetupTokenResult.Failure(
                "PendingDeviceRequestIdRequired",
                "Pending device request id is required before issuing a first-manager setup token.");
        }

        var managerDisplayName = NormalizeText(command.ManagerDisplayName);

        if (managerDisplayName is null)
        {
            return IssueLocalServerFirstManagerSetupTokenResult.Failure(
                "ManagerDisplayNameRequired",
                "Manager display name is required before issuing a first-manager setup token.");
        }

        var purpose = NormalizePurpose(command.Purpose);

        if (purpose is null)
        {
            return IssueLocalServerFirstManagerSetupTokenResult.Failure(
                "FirstManagerSetupTokenPurposeInvalid",
                "First-manager setup token purpose is not supported.");
        }

        var recoveryReason = NormalizeText(command.RecoveryReason);
        if (string.Equals(
                purpose,
                LocalServerFirstManagerSetupTokenPurposes.ManagerRecovery,
                StringComparison.Ordinal)
            && recoveryReason is null)
        {
            return IssueLocalServerFirstManagerSetupTokenResult.Failure(
                "ManagerRecoveryReasonRequired",
                "A recovery reason is required before issuing a manager recovery setup token.");
        }

        var installation = await _installations.GetByInstallationIdAsync(
            installationId,
            cancellationToken);

        if (installation is null)
        {
            return IssueLocalServerFirstManagerSetupTokenResult.Failure(
                "InstallationNotRegistered",
                "Installation must be registered before a first-manager setup token can be issued.");
        }

        if (installation.ClientId != command.ClientId)
        {
            return IssueLocalServerFirstManagerSetupTokenResult.Failure(
                "InstallationClientMismatch",
                "Installation id is already bound to another client.");
        }

        var now = _clock.UtcNow;
        var expiresAtUtc = now.AddHours(Math.Clamp(command.ExpiresInHours, 1, 168));
        var createdBy = NormalizeText(command.CreatedBy) ?? "SafarSuite Control Cloud";
        var payload = new LocalServerFirstManagerSetupTokenPayloadResponse(
            LocalServerPairingFormats.FirstManagerSetupTokenVersion,
            Guid.NewGuid(),
            command.ClientId,
            installationId,
            command.PendingDeviceRequestId,
            ResolveAllowedActions(purpose),
            managerDisplayName,
            NormalizeText(command.ManagerEmail),
            createdBy,
            now,
            expiresAtUtc,
            purpose,
            recoveryReason);
        var signed = _signer.Sign(payload);

        try
        {
            await _issues.AddAsync(
                ControlCloudFirstManagerSetupTokenIssue.Create(
                    payload.TokenId,
                    payload.ClientId,
                    payload.InstallationId,
                    payload.PendingDeviceRequestId,
                    payload.ManagerDisplayName,
                    payload.ManagerEmail,
                    payload.CreatedBy,
                    signed.Signature.KeyId,
                    signed.Signature.PayloadSha256,
                    payload.IssuedAtUtc,
                    payload.ExpiresAtUtc),
                cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return IssueLocalServerFirstManagerSetupTokenResult.Failure(
                "FirstManagerSetupTokenInvalid",
                exception.Message);
        }

        await ControlCloudAuditWriter.TryRecordAsync(
            _audit,
            new ClientPortalAuditRecord(
                Guid.NewGuid(),
                command.ClientId,
                InvitationId: null,
                UserId: null,
                SubjectEmail: payload.ManagerEmail ?? "",
                ClientPortalAuditEventTypes.FirstManagerSetupTokenIssued,
                ControlCloudAuditWriter.NormalizeActor(createdBy, ClientPortalAuditActors.ControlDesk),
                $"First-manager setup token '{payload.TokenId}' issued for installation '{installationId}', pending device request '{payload.PendingDeviceRequestId}', manager '{payload.ManagerDisplayName}', purpose '{payload.Purpose}', signing key '{signed.Signature.KeyId}', and expiry '{payload.ExpiresAtUtc:O}'.",
                now),
            cancellationToken);

        return IssueLocalServerFirstManagerSetupTokenResult.Success(
            new IssueLocalServerFirstManagerSetupTokenResponse(
                payload.TokenId,
                payload.ClientId,
                payload.InstallationId,
                payload.PendingDeviceRequestId,
                payload.ManagerDisplayName,
                payload.ManagerEmail,
                payload.CreatedBy,
                signed.Signature.KeyId,
                signed.Signature.PayloadSha256,
                payload.IssuedAtUtc,
                payload.ExpiresAtUtc,
                signed,
                payload.Purpose,
                payload.RecoveryReason,
                payload.AllowedActions));
    }

    private static string? NormalizePurpose(string? value)
    {
        var normalized = NormalizeText(value)
            ?? LocalServerFirstManagerSetupTokenPurposes.FirstManagerBootstrap;

        return normalized switch
        {
            LocalServerFirstManagerSetupTokenPurposes.FirstManagerBootstrap
                => LocalServerFirstManagerSetupTokenPurposes.FirstManagerBootstrap,
            LocalServerFirstManagerSetupTokenPurposes.ManagerRecovery
                => LocalServerFirstManagerSetupTokenPurposes.ManagerRecovery,
            _ => null
        };
    }

    private static IReadOnlyCollection<string> ResolveAllowedActions(string purpose)
    {
        if (string.Equals(
                purpose,
                LocalServerFirstManagerSetupTokenPurposes.ManagerRecovery,
                StringComparison.Ordinal))
        {
            return
            [
                LocalServerFirstManagerSetupTokenActions.RecoverManagerAccess,
                LocalServerFirstManagerSetupTokenActions.ApproveManagerDevice
            ];
        }

        return
        [
            LocalServerFirstManagerSetupTokenActions.CreateFirstManager,
            LocalServerFirstManagerSetupTokenActions.ApproveFirstDevice
        ];
    }

    private static string? NormalizeText(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
