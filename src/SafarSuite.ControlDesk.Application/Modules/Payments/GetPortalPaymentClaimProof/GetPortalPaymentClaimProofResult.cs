namespace SafarSuite.ControlDesk.Application.Modules.Payments.GetPortalPaymentClaimProof;

public sealed record GetPortalPaymentClaimProofResult(
    byte[] Content,
    string ContentType,
    string FileName);
