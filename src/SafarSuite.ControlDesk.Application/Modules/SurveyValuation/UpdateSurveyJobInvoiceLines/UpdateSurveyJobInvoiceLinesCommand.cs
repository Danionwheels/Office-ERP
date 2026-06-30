using SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

namespace SafarSuite.ControlDesk.Application.Modules.SurveyValuation.UpdateSurveyJobInvoiceLines;

public sealed record UpdateSurveyJobInvoiceLinesCommand(
    Guid SurveyJobId,
    IReadOnlyCollection<SurveyJobInvoiceLineCommand>? InvoiceLines);

public sealed record SurveyJobInvoiceLineCommand(
    int SequenceNumber,
    SurveyInvoiceLineDescriptionType DescriptionType,
    string Description,
    decimal Amount,
    string CurrencyCode,
    string? BillingHeadCode,
    string? TaxCode,
    string? CategoryCode);
