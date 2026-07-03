namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetJournalEntrySourceDocument;

public sealed record JournalEntrySourceDocumentResult(
    Guid JournalEntryId,
    string SourceType,
    string? SourceReference,
    bool IsResolved,
    string? DocumentKind,
    Guid? DocumentId,
    Guid? ClientId,
    Guid? RelatedInvoiceId,
    string? Reference,
    string? Status,
    DateOnly? DocumentDate,
    string? CurrencyCode,
    decimal? Amount,
    string? Label,
    string? DashboardModule,
    string? DashboardStep,
    string? Message);
