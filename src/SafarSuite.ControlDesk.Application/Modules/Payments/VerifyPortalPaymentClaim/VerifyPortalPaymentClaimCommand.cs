namespace SafarSuite.ControlDesk.Application.Modules.Payments.VerifyPortalPaymentClaim;

public sealed record VerifyPortalPaymentClaimCommand(
    Guid ClaimId,
    Guid CashOrBankAccountId,
    Guid AccountsReceivableAccountId,
    DateOnly PostingDate,
    string? DecisionNote);
