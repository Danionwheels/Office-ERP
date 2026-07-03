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
    string? ProductModuleCode,
    string? DescriptionOverride,
    decimal UnitPriceAmount,
    string CurrencyCode,
    decimal Quantity,
    decimal TaxPercent,
    string BillingCycle,
    int BillingDayOfMonth,
    DateOnly EffectiveStartsOn,
    DateOnly EffectiveEndsOn);

public sealed record CreateClientChargeRuleResponse(
    Guid ClientChargeRuleId,
    Guid ClientId,
    Guid? ContractId,
    Guid ChargeCodeId,
    string? ProductModuleCode,
    decimal UnitPriceAmount,
    string CurrencyCode,
    decimal Quantity,
    decimal TaxPercent,
    decimal TaxAmount,
    decimal LineAmount,
    decimal TotalLineAmount,
    string BillingCycle,
    int BillingDayOfMonth,
    DateOnly EffectiveStartsOn,
    DateOnly EffectiveEndsOn,
    string Status);

public sealed record ListClientChargeRulesResponse(
    DateOnly EffectiveOn,
    IReadOnlyCollection<ClientChargeRuleLookupResponse> ChargeRules);

public sealed record ClientChargeRuleLookupResponse(
    Guid ClientChargeRuleId,
    Guid ClientId,
    Guid? ContractId,
    Guid ChargeCodeId,
    string? ProductModuleCode,
    decimal UnitPriceAmount,
    string CurrencyCode,
    decimal Quantity,
    decimal TaxPercent,
    decimal TaxAmount,
    decimal LineAmount,
    decimal TotalLineAmount,
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

public sealed record InvoiceDocumentResponse(
    GenerateInvoiceDraftResponse Invoice,
    IssueInvoiceResponse? IssuedInvoice,
    VoidInvoiceResponse? VoidedInvoice,
    IssueCreditNoteResponse? CreditNote);

public sealed record CreditNoteDocumentResponse(
    GenerateInvoiceDraftResponse Invoice,
    IssueCreditNoteResponse CreditNote);

public sealed record GenerateInvoiceDraftLineResponse(
    Guid? ChargeCodeId,
    string? ProductModuleCode,
    string LineType,
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

public sealed record VoidInvoiceRequest(
    DateOnly VoidDate,
    string Reason);

public sealed record VoidInvoiceResponse(
    Guid InvoiceId,
    string InvoiceNumber,
    string InvoiceStatus,
    Guid OriginalJournalEntryId,
    Guid ReversalJournalEntryId,
    string ReversalJournalEntryStatus,
    DateOnly VoidDate,
    decimal TotalDebit,
    decimal TotalCredit,
    string CurrencyCode,
    IReadOnlyCollection<VoidInvoiceJournalLineResponse> JournalLines);

public sealed record VoidInvoiceJournalLineResponse(
    Guid LedgerAccountId,
    decimal Debit,
    decimal Credit,
    string? Description);

public sealed record IssueCreditNoteRequest(
    string CreditNoteNumber,
    DateOnly CreditDate,
    string Reason);

public sealed record IssueCreditNoteResponse(
    Guid CreditNoteId,
    Guid InvoiceId,
    string CreditNoteNumber,
    string InvoiceNumber,
    string CreditNoteStatus,
    DateOnly CreditDate,
    decimal Amount,
    string CurrencyCode,
    Guid JournalEntryId,
    string JournalEntryStatus,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyCollection<IssueCreditNoteJournalLineResponse> JournalLines);

public sealed record IssueCreditNoteJournalLineResponse(
    Guid LedgerAccountId,
    decimal Debit,
    decimal Credit,
    string? Description);
