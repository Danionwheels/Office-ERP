namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountActivity;

public sealed record GetLedgerAccountActivityResult(
    Guid LedgerAccountId,
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? CurrencyCode,
    decimal OpeningBalance,
    decimal PeriodDebit,
    decimal PeriodCredit,
    decimal EndingBalance,
    IReadOnlyCollection<LedgerAccountActivityLineResult> Lines);

public sealed record LedgerAccountActivityLineResult(
    Guid JournalEntryId,
    DateOnly EntryDate,
    string SourceType,
    string? SourceReference,
    string? Memo,
    string Status,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance,
    string CurrencyCode,
    string? Description);
