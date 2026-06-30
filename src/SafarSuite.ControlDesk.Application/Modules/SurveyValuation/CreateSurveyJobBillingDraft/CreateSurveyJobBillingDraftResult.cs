using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.SurveyJobEntry;

namespace SafarSuite.ControlDesk.Application.Modules.SurveyValuation.CreateSurveyJobBillingDraft;

public sealed record CreateSurveyJobBillingDraftResult(
    Guid InvoiceId,
    string InvoiceNumber,
    string Status,
    decimal TotalAmount,
    decimal BalanceDue,
    string CurrencyCode,
    IReadOnlyCollection<CreateSurveyJobBillingDraftLineResult> Lines,
    SurveyJobEntryDto SurveyJob);

public sealed record CreateSurveyJobBillingDraftLineResult(
    Guid ChargeCodeId,
    string ChargeCode,
    string Description,
    decimal Amount,
    string CurrencyCode);
