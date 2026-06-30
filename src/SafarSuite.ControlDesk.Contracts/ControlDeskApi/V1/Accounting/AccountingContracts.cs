namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Accounting;

public sealed record CreateLedgerAccountRequest(
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    Guid? ParentAccountId = null,
    bool IsPostingAccount = true);

public sealed record CreateLedgerAccountResponse(
    Guid LedgerAccountId,
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    bool IsPostingAccount,
    string Status);

public sealed record ListJournalEntriesResponse(
    IReadOnlyCollection<JournalEntrySummaryResponse> Entries);

public sealed record JournalEntrySummaryResponse(
    Guid JournalEntryId,
    DateOnly EntryDate,
    string CurrencyCode,
    string SourceType,
    string? SourceReference,
    string? Memo,
    string Status,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyCollection<JournalEntryLineResponse> Lines);

public sealed record JournalEntryLineResponse(
    Guid LedgerAccountId,
    decimal Debit,
    decimal Credit,
    string? Description);

public sealed record LedgerAccountActivityResponse(
    Guid LedgerAccountId,
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? CurrencyCode,
    decimal EndingBalance,
    IReadOnlyCollection<LedgerAccountActivityLineResponse> Lines);

public sealed record LedgerAccountActivityLineResponse(
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
