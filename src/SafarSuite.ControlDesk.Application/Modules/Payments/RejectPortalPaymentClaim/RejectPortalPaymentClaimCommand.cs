namespace SafarSuite.ControlDesk.Application.Modules.Payments.RejectPortalPaymentClaim;

public sealed record RejectPortalPaymentClaimCommand(Guid ClaimId, string Reason);
