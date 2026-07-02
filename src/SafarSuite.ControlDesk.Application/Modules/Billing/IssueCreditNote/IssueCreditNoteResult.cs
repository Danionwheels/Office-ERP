namespace SafarSuite.ControlDesk.Application.Modules.Billing.IssueCreditNote;

public sealed record IssueCreditNoteResult(
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
    IReadOnlyCollection<IssueCreditNoteJournalLineResult> JournalLines);

public sealed record IssueCreditNoteJournalLineResult(
    Guid LedgerAccountId,
    decimal Debit,
    decimal Credit,
    string? Description);
