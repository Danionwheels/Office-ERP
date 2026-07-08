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
            [
                LocalServerFirstManagerSetupTokenActions.CreateFirstManager,
                LocalServerFirstManagerSetupTokenActions.ApproveFirstDevice
            ],
            managerDisplayName,
            NormalizeText(command.ManagerEmail),
            createdBy,
            now,
            expiresAtUtc);
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
                $"First-manager setup token '{payload.TokenId}' issued for installation '{installationId}', pending device request '{payload.PendingDeviceRequestId}', manager '{payload.ManagerDisplayName}', signing key '{signed.Signature.KeyId}', and expiry '{payload.ExpiresAtUtc:O}'.",
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
                signed));
    }

    private static string? NormalizeText(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
