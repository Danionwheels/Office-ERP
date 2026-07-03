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
    Guid? ParentAccountId,
    bool IsPostingAccount,
    string Status);

public sealed record UpdateLedgerAccountRequest(
    string Name,
    bool IsPostingAccount,
    string Status);

public sealed record UpdateLedgerAccountResponse(
    Guid LedgerAccountId,
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    Guid? ParentAccountId,
    bool IsPostingAccount,
    string Status,
    DateTimeOffset CreatedAtUtc);

public sealed record ListLedgerAccountsResponse(
    string CompanyCode,
    IReadOnlyCollection<LedgerAccountSummaryResponse> Accounts);

public sealed record LedgerAccountSummaryResponse(
    Guid LedgerAccountId,
    string Code,
    string DisplayCode,
    string Name,
    string Type,
    string NormalBalance,
    Guid? ParentAccountId,
    bool IsPostingAccount,
    string Status,
    DateTimeOffset CreatedAtUtc,
    string? RangeRole,
    string? RangeDisplayName);

public sealed record SuggestLedgerAccountCodeResponse(
    string CompanyCode,
    string Role,
    string SuggestedCode,
    string DisplayCode,
    string Type,
    string NormalBalance,
    bool IsPostingAccount,
    string RangeStart,
    string RangeEnd,
    string? ParentCode);

public sealed record ConfigureAccountCodeRangeRequest(
    string DisplayName,
    string SearchPrefix,
    string RangeStart,
    string RangeEnd,
    int CodeLength,
    string AccountType,
    string NormalBalance,
    bool IsPostingAccount,
    string? ParentCode = null,
    bool IsActive = true);

public sealed record AccountCodeRangeResponse(
    Guid AccountCodeRangeId,
    string CompanyCode,
    string Role,
    string DisplayName,
    string SearchPrefix,
    string RangeStart,
    string RangeEnd,
    int CodeLength,
    string AccountType,
    string NormalBalance,
    bool IsPostingAccount,
    string? ParentCode,
    bool IsActive);

public sealed record ListAccountCodeRangesResponse(
    string CompanyCode,
    IReadOnlyCollection<AccountCodeRangeResponse> Ranges);

public sealed record ListJournalEntriesResponse(
    IReadOnlyCollection<JournalEntrySummaryResponse> Entries);

public sealed record PostManualJournalEntryRequest(
    DateOnly EntryDate,
    string CurrencyCode,
    string? SourceReference,
    string? Memo,
    IReadOnlyCollection<PostManualJournalEntryLineRequest> Lines);

public sealed record PostManualJournalEntryLineRequest(
    Guid LedgerAccountId,
    decimal Debit,
    decimal Credit,
    string? Description);

public sealed record VoidManualJournalEntryRequest(
    DateOnly VoidDate,
    string Reason);

public sealed record VoidManualJournalEntryResponse(
    Guid OriginalJournalEntryId,
    Guid ReversalJournalEntryId,
    string OriginalJournalEntryStatus,
    string ReversalJournalEntryStatus,
    DateOnly VoidDate,
    decimal TotalDebit,
    decimal TotalCredit,
    string CurrencyCode,
    IReadOnlyCollection<JournalEntryLineResponse> Lines);

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
