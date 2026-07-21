namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.RejectPortalPaymentClaim;

public sealed record RejectClientPortalPaymentClaimCommand(
    Guid ClaimId,
    string Reason,
    string Actor);
