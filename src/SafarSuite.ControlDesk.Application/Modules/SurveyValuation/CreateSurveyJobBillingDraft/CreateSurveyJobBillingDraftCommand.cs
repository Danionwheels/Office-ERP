namespace SafarSuite.ControlDesk.Application.Modules.SurveyValuation.CreateSurveyJobBillingDraft;

public sealed record CreateSurveyJobBillingDraftCommand(
    Guid SurveyJobId,
    Guid ClientId,
    Guid ContractId,
    string InvoiceNumber,
    DateOnly IssueDate,
    DateOnly DueDate,
    string CurrencyCode);
