using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.GetPortalPaymentClaimProof;

public sealed class GetClientPortalPaymentClaimProofHandler
{
    private readonly IClientPortalPaymentClaimRepository _claims;
    private readonly IClientPortalAttachmentRepository _attachments;

    public GetClientPortalPaymentClaimProofHandler(
        IClientPortalPaymentClaimRepository claims,
        IClientPortalAttachmentRepository attachments)
    {
        _claims = claims;
        _attachments = attachments;
    }

    public async Task<ClientPortalPaymentOperationResult<ControlCloudClientPortalAttachment>> HandleAsync(
        Guid claimId,
        Guid? requiredClientId,
        CancellationToken cancellationToken = default)
    {
        var claim = await _claims.GetByIdAsync(claimId, cancellationToken);
        if (claim is null
            || (requiredClientId is not null && claim.ClientId != requiredClientId.Value)
            || claim.ProofAttachmentId is null)
        {
            return Failure();
        }

        var attachment = await _attachments.GetByIdAsync(claim.ProofAttachmentId.Value, cancellationToken);
        return attachment is not null && attachment.ClientId == claim.ClientId
            ? ClientPortalPaymentOperationResult<ControlCloudClientPortalAttachment>.Success(attachment)
            : Failure();
    }

    private static ClientPortalPaymentOperationResult<ControlCloudClientPortalAttachment> Failure() =>
        ClientPortalPaymentOperationResult<ControlCloudClientPortalAttachment>.Failure(
            "PaymentProofNotFound",
            "Payment proof was not found.");
}
