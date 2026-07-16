using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.RejectPortalPaymentClaim;

public sealed class RejectClientPortalPaymentClaimHandler
{
    private readonly IClientPortalPaymentClaimRepository _claims;
    private readonly IClientPortalAttachmentRepository _attachments;
    private readonly IClientPortalAuditRecorder _audit;
    private readonly IControlCloudUnitOfWork _unitOfWork;
    private readonly IControlCloudClock _clock;

    public RejectClientPortalPaymentClaimHandler(
        IClientPortalPaymentClaimRepository claims,
        IClientPortalAttachmentRepository attachments,
        IClientPortalAuditRecorder audit,
        IControlCloudUnitOfWork unitOfWork,
        IControlCloudClock clock)
    {
        _claims = claims;
        _attachments = attachments;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ClientPortalPaymentOperationResult<ClientPortalPaymentClaimView>> HandleAsync(
        RejectClientPortalPaymentClaimCommand command,
        CancellationToken cancellationToken = default)
    {
        var claim = await _claims.GetByIdAsync(command.ClaimId, cancellationToken);
        if (claim is null)
        {
            return Failure("PaymentClaimNotFound", "Payment claim was not found.");
        }

        try
        {
            var now = _clock.UtcNow;
            claim.Reject(command.Reason, now);
            await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    await _claims.SaveAsync(claim, token);
                    await ControlCloudAuditWriter.TryRecordAsync(
                        _audit,
                        new ClientPortalAuditRecord(
                            Guid.NewGuid(),
                            claim.ClientId,
                            null,
                            null,
                            "",
                            ClientPortalAuditEventTypes.PaymentClaimRejected,
                            string.IsNullOrWhiteSpace(command.Actor) ? ClientPortalAuditActors.ControlCloud : command.Actor.Trim(),
                            $"Payment claim {claim.ClaimId:D} was rejected.",
                            now),
                        token);
                },
                cancellationToken);
            var attachment = claim.ProofAttachmentId is null
                ? null
                : await _attachments.GetByIdAsync(claim.ProofAttachmentId.Value, cancellationToken);
            return ClientPortalPaymentOperationResult<ClientPortalPaymentClaimView>.Success(
                new ClientPortalPaymentClaimView(claim, attachment));
        }
        catch (ArgumentException exception)
        {
            return Failure("PaymentClaimRejectionInvalid", exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Failure("PaymentClaimConflict", exception.Message);
        }
    }

    private static ClientPortalPaymentOperationResult<ClientPortalPaymentClaimView> Failure(
        string code,
        string detail) =>
        ClientPortalPaymentOperationResult<ClientPortalPaymentClaimView>.Failure(code, detail);
}
