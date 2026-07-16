using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.GetPortalPaymentClaim;

public sealed class GetClientPortalPaymentClaimHandler
{
    private readonly IClientPortalPaymentClaimRepository _claims;
    private readonly IClientPortalAttachmentRepository _attachments;

    public GetClientPortalPaymentClaimHandler(
        IClientPortalPaymentClaimRepository claims,
        IClientPortalAttachmentRepository attachments)
    {
        _claims = claims;
        _attachments = attachments;
    }

    public async Task<ClientPortalPaymentOperationResult<ClientPortalPaymentClaimView>> HandleAsync(
        Guid claimId,
        Guid? requiredClientId,
        CancellationToken cancellationToken = default)
    {
        if (claimId == Guid.Empty)
        {
            return Failure("PaymentClaimIdRequired", "Payment claim id is required.");
        }

        var claim = await _claims.GetByIdAsync(claimId, cancellationToken);
        if (claim is null || (requiredClientId is not null && claim.ClientId != requiredClientId.Value))
        {
            return Failure("PaymentClaimNotFound", "Payment claim was not found.");
        }

        var attachment = claim.ProofAttachmentId is null
            ? null
            : await _attachments.GetByIdAsync(claim.ProofAttachmentId.Value, cancellationToken);
        return ClientPortalPaymentOperationResult<ClientPortalPaymentClaimView>.Success(
            new ClientPortalPaymentClaimView(claim, attachment));
    }

    private static ClientPortalPaymentOperationResult<ClientPortalPaymentClaimView> Failure(
        string code,
        string detail) =>
        ClientPortalPaymentOperationResult<ClientPortalPaymentClaimView>.Failure(code, detail);
}
