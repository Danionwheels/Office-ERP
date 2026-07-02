using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed class ResendClientPortalInvitationHandler
{
    private readonly IControlCloudClientCommercialProjectionRepository _projections;
    private readonly IClientPortalIdentityRepository _identities;
    private readonly IClientPortalCredentialService _credentials;
    private readonly IClientPortalAuditRecorder _audit;
    private readonly IControlCloudUnitOfWork _unitOfWork;
    private readonly IControlCloudClock _clock;

    public ResendClientPortalInvitationHandler(
        IControlCloudClientCommercialProjectionRepository projections,
        IClientPortalIdentityRepository identities,
        IClientPortalCredentialService credentials,
        IClientPortalAuditRecorder audit,
        IControlCloudUnitOfWork unitOfWork,
        IControlCloudClock clock)
    {
        _projections = projections;
        _identities = identities;
        _credentials = credentials;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ManageClientPortalInvitationResult> HandleAsync(
        ResendClientPortalInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ClientId == Guid.Empty)
        {
            return ManageClientPortalInvitationResult.Failure(
                "ClientIdRequired",
                "Client id is required before resending a portal invitation.");
        }

        if (command.InvitationId == Guid.Empty)
        {
            return ManageClientPortalInvitationResult.Failure(
                "InvitationIdRequired",
                "Invitation id is required before resending a portal invitation.");
        }

        var projection = await _projections.GetByClientIdAsync(command.ClientId, cancellationToken);

        if (projection is null)
        {
            return ManageClientPortalInvitationResult.Failure(
                "ClientNotFound",
                "Client is not available in Control Cloud yet.");
        }

        var token = _credentials.CreateInvitationToken();
        var now = _clock.UtcNow;
        var expiresAtUtc = now.AddDays(Math.Clamp(command.ExpiresInDays, 1, 30));

        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(
                async transactionToken =>
                {
                    var invitation = await _identities.GetInvitationByIdAsync(
                        command.InvitationId,
                        transactionToken);

                    if (invitation is null)
                    {
                        return ManageClientPortalInvitationResult.Failure(
                            "InvitationNotFound",
                            "Portal invitation was not found.");
                    }

                    if (invitation.ClientId != command.ClientId)
                    {
                        return ManageClientPortalInvitationResult.Failure(
                            "InvitationClientMismatch",
                            "Portal invitation belongs to another client.");
                    }

                    invitation.Resend(
                        _credentials.HashSecret(token),
                        NormalizeCreatedBy(command.CreatedBy),
                        now,
                        expiresAtUtc);

                    await _identities.SaveInvitationAsync(invitation, transactionToken);
                    await ControlCloudAuditWriter.TryRecordAsync(
                        _audit,
                        new ClientPortalAuditRecord(
                            Guid.NewGuid(),
                            invitation.ClientId,
                            invitation.InvitationId,
                            null,
                            invitation.Email,
                            ClientPortalAuditEventTypes.InvitationResent,
                            NormalizeCreatedBy(command.CreatedBy),
                            "Client portal invitation token was rotated and resent.",
                            now),
                        transactionToken);

                    return ManageClientPortalInvitationResult.Success(
                        ClientPortalInvitationResultMapper.ToItem(invitation, token));
                },
                cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return ManageClientPortalInvitationResult.Failure(
                "InvitationNotUsable",
                exception.Message);
        }
    }

    private static string NormalizeCreatedBy(string createdBy)
    {
        return string.IsNullOrWhiteSpace(createdBy)
            ? "SafarSuite Control Cloud"
            : createdBy.Trim();
    }
}
