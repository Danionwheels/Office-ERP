namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.CreatePortalPaymentClaim;

public sealed record CreateClientPortalPaymentClaimCommand(
    Guid ClientId,
    Guid SubmittedByUserId,
    Guid InvoiceId,
    decimal Amount,
    string TransferReferenceNumber,
    Guid? ProofAttachmentId);
