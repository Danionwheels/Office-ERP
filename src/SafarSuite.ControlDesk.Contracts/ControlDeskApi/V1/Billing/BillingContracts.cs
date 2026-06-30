namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Billing;

public sealed record CreateChargeCodeRequest(
    string Code,
    string Name,
    string? Description,
    decimal DefaultUnitPriceAmount,
    string CurrencyCode,
    Guid RevenueAccountId,
    Guid? TaxAccountId = null);

public sealed record CreateChargeCodeResponse(
    Guid ChargeCodeId,
    string Code,
    string Name,
    decimal DefaultUnitPriceAmount,
    string CurrencyCode,
    Guid RevenueAccountId,
    Guid? TaxAccountId,
    string Status);

public sealed record ListChargeCodesResponse(
    IReadOnlyCollection<ChargeCodeLookupResponse> ChargeCodes);

public sealed record ChargeCodeLookupResponse(
    Guid ChargeCodeId,
    string Code,
    string Name,
    decimal DefaultUnitPriceAmount,
    string CurrencyCode,
    Guid RevenueAccountId,
    Guid? TaxAccountId,
    string Status);

public sealed record CreateClientChargeRuleRequest(
    Guid ClientId,
    Guid? ContractId,
    Guid ChargeCodeId,
    string? DescriptionOverride,
    decimal UnitPriceAmount,
    string CurrencyCode,
    decimal Quantity,
    string BillingCycle,
    int BillingDayOfMonth,
    DateOnly EffectiveStartsOn,
    DateOnly EffectiveEndsOn);

public sealed record CreateClientChargeRuleResponse(
    Guid ClientChargeRuleId,
    Guid ClientId,
    Guid? ContractId,
    Guid ChargeCodeId,
    decimal UnitPriceAmount,
    string CurrencyCode,
    decimal Quantity,
    decimal LineAmount,
    string BillingCycle,
    int BillingDayOfMonth,
    DateOnly EffectiveStartsOn,
    DateOnly EffectiveEndsOn,
    string Status);

public sealed record GenerateInvoiceDraftRequest(
    Guid ClientId,
    Guid ContractId,
    string InvoiceNumber,
    DateOnly IssueDate,
    DateOnly DueDate,
    DateOnly BillingDate,
    string CurrencyCode);

public sealed record GenerateInvoiceDraftResponse(
    Guid InvoiceId,
    Guid ClientId,
    Guid ContractId,
    string InvoiceNumber,
    DateOnly IssueDate,
    DateOnly DueDate,
    DateOnly BillingDate,
    decimal TotalAmount,
    decimal BalanceDue,
    string CurrencyCode,
    string Status,
    IReadOnlyCollection<GenerateInvoiceDraftLineResponse> Lines);

public sealed record GenerateInvoiceDraftLineResponse(
    Guid? ChargeCodeId,
    string Description,
    decimal Amount,
    string CurrencyCode);

public sealed record IssueInvoiceRequest(
    Guid? AccountsReceivableAccountId,
    DateOnly PostingDate);

public sealed record IssueInvoiceResponse(
    Guid InvoiceId,
    string InvoiceNumber,
    string InvoiceStatus,
    Guid JournalEntryId,
    string JournalEntryStatus,
    DateOnly PostingDate,
    decimal TotalDebit,
    decimal TotalCredit,
    string CurrencyCode,
    IReadOnlyCollection<IssueInvoiceJournalLineResponse> JournalLines);

public sealed record IssueInvoiceJournalLineResponse(
    Guid LedgerAccountId,
    decimal Debit,
    decimal Credit,
    string? Description);
