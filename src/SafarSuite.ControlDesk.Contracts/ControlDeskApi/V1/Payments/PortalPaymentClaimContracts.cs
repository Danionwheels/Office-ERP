using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Payments;

public sealed record ImportPortalPaymentClaimsResponse(
    Guid ClientId,
    int RetrievedCount,
    int ImportedCount,
    int AlreadyImportedCount,
    int IgnoredCount,
    IReadOnlyCollection<ClientPortalPaymentClaimResponse> Claims);

public sealed record VerifyPortalPaymentClaimRequest(
    Guid CashOrBankAccountId,
    Guid AccountsReceivableAccountId,
    DateOnly PostingDate,
    string? DecisionNote);

public sealed record VerifyPortalPaymentClaimResponse(
    ClientPortalPaymentClaimResponse Claim,
    RecordInvoicePaymentResponse Payment);

public sealed record UpdateProviderBankDetailsRequest(
    string BankName,
    string AccountTitle,
    string AccountNumber,
    string Iban,
    string BranchOrRoutingInfo);
