using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.Common;

public static class PortalPaymentClaimResultFactory
{
    public static PortalPaymentClaimResult From(PortalPaymentClaim claim)
    {
        PortalPaymentClaimProofSummaryResult? proof = null;

        if (claim.ProofAttachmentId.HasValue
            && claim.ProofFileName is not null
            && claim.ProofContentType is not null
            && claim.ProofSizeBytes.HasValue
            && claim.ProofUploadedAtUtc.HasValue)
        {
            proof = new PortalPaymentClaimProofSummaryResult(
                claim.ProofAttachmentId.Value,
                claim.ProofFileName,
                claim.ProofContentType,
                claim.ProofSizeBytes.Value,
                claim.ProofUploadedAtUtc.Value);
        }

        return new PortalPaymentClaimResult(
            claim.Id.Value,
            claim.ClientId.Value,
            claim.InvoiceId.Value,
            claim.InvoiceNumber,
            claim.Amount.Amount,
            claim.Amount.CurrencyCode,
            claim.TransferReferenceNumber,
            claim.ProofAttachmentId,
            proof,
            claim.Status.ToString(),
            claim.SubmittedAtUtc,
            claim.ReviewedAtUtc,
            claim.RejectionReason);
    }
}
