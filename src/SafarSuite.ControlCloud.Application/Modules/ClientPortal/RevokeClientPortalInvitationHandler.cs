using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed class RevokeClientPortalInvitationHandler
{
    private readonly IClientPortalIdentityRepository _identities;
    private readonly IClientPortalAuditRecorder _audit;
    private readonly IControlCloudUnitOfWork _unitOfWork;
    private readonly IControlCloudClock _clock;

    public RevokeClientPortalInvitationHandler(
        IClientPortalIdentityRepository identities,
        IClientPortalAuditRecorder audit,
        IControlCloudUnitOfWork unitOfWork,
        IControlCloudClock clock)
    {
        _identities = identities;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ManageClientPortalInvitationResult> HandleAsync(
        RevokeClientPortalInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ClientId == Guid.Empty)
        {
            return ManageClientPortalInvitationResult.Failure(
                "ClientIdRequired",
                "Client id is required before revoking a portal invitation.");
        }

        if (command.InvitationId == Guid.Empty)
        {
            return ManageClientPortalInvitationResult.Failure(
                "InvitationIdRequired",
                "Invitation id is required before revoking a portal invitation.");
        }

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

                    invitation.Revoke();
                    await _identities.SaveInvitationAsync(invitation, transactionToken);
                    await ControlCloudAuditWriter.TryRecordAsync(
                        _audit,
                        new ClientPortalAuditRecord(
                            Guid.NewGuid(),
                            invitation.ClientId,
                            invitation.InvitationId,
                            null,
                            invitation.Email,
                            ClientPortalAuditEventTypes.InvitationRevoked,
                            ClientPortalAuditActors.ControlDesk,
                            "Client portal invitation was revoked.",
                            _clock.UtcNow),
                        transactionToken);

                    return ManageClientPortalInvitationResult.Success(
                        ClientPortalInvitationResultMapper.ToItem(invitation));
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
}
