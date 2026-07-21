namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Accounting;

public sealed record CreateLedgerAccountRequest(
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    Guid? ParentAccountId = null,
    bool IsPostingAccount = true,
    string? Level = null);

public sealed record CreateLedgerAccountResponse(
    Guid LedgerAccountId,
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    string Level,
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
    string Level,
    Guid? ParentAccountId,
    bool IsPostingAccount,
    string Status,
    DateTimeOffset CreatedAtUtc);

public sealed record ListLedgerAccountsResponse(
    string CompanyCode,
    IReadOnlyCollection<LedgerAccountSummaryResponse> Accounts);

public sealed record LedgerAccountReconciliationResponse(
    string CompanyCode,
    int AccountCount,
    int IssueCount,
    IReadOnlyCollection<LedgerAccountReconciliationItemResponse> Items);

public sealed record LedgerAccountReconciliationItemResponse(
    Guid LedgerAccountId,
    string Code,
    string DisplayCode,
    string Name,
    string Type,
    string NormalBalance,
    string Level,
    Guid? ParentAccountId,
    bool IsPostingAccount,
    string Status,
    string? RangeRole,
    string? RangeDisplayName,
    IReadOnlyCollection<LedgerAccountReconciliationIssueResponse> Issues);

public sealed record LedgerAccountReconciliationIssueResponse(
    string Severity,
    string Code,
    string Message);

public sealed record LedgerAccountRepairPlanResponse(
    string CompanyCode,
    int AccountCount,
    int IssueCount,
    int ActionCount,
    IReadOnlyCollection<LedgerAccountRepairPlanItemResponse> Items);

public sealed record LedgerAccountRepairPlanItemResponse(
    Guid LedgerAccountId,
    string Code,
    string DisplayCode,
    string Name,
    string Type,
    string NormalBalance,
    string Level,
    Guid? ParentAccountId,
    bool IsPostingAccount,
    string Status,
    string? RangeRole,
    string? RangeDisplayName,
    IReadOnlyCollection<LedgerAccountRepairActionResponse> Actions);

public sealed record LedgerAccountRepairActionResponse(
    string IssueCode,
    string Severity,
    string ActionCode,
    string Title,
    string Description,
    string RepairMode,
    bool IsAutomatable,
    string? CurrentValue,
    string? SuggestedValue,
    IReadOnlyCollection<string> Notes);

public sealed record LedgerAccountSummaryResponse(
    Guid LedgerAccountId,
    string Code,
    string DisplayCode,
    string Name,
    string Type,
    string NormalBalance,
    string Level,
    Guid? ParentAccountId,
    bool IsPostingAccount,
    string Status,
    DateTimeOffset CreatedAtUtc,
    string? RangeRole,
    string? RangeDisplayName);

public sealed record PreviewChartOfAccountsImportTextRequest(
    string? CompanyCode,
    string ImportText,
    string? Delimiter);

public sealed record PreviewChartOfAccountsImportTextResponse(
    string CompanyCode,
    string Format,
    int ParsedLineCount,
    int IgnoredLineCount,
    bool CanImport,
    int InsertCount,
    int UpdateCount,
    int NoChangeCount,
    int RejectCount,
    int WarningCount,
    int IssueCount,
    IReadOnlyCollection<ChartOfAccountsImportParseIssueResponse> ParseIssues,
    IReadOnlyCollection<ChartOfAccountsImportRowResponse> Rows);

public sealed record ChartOfAccountsImportParseIssueResponse(
    int LineNumber,
    string Column,
    string Message,
    string? RawValue);

public sealed record ChartOfAccountsImportRowResponse(
    int LineNumber,
    string Action,
    string Code,
    string DisplayCode,
    string Name,
    string? ImportedLevel,
    string ResolvedLevel,
    string Type,
    string NormalBalance,
    bool IsPostingAccount,
    string? ParentCode,
    Guid? ParentAccountId,
    string? ParentSource,
    string? CurrencyCode,
    Guid? ExistingLedgerAccountId,
    string? ExistingStatus,
    string? RangeRole,
    string? RangeDisplayName,
    IReadOnlyCollection<ChartOfAccountsImportIssueResponse> Issues);

public sealed record ChartOfAccountsImportIssueResponse(
    string Severity,
    string Code,
    string Message);

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
    string? ParentCode,
    Guid? ParentAccountId,
    string? ParentAccountCode,
    string? ParentAccountName);

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

public sealed record AccountCodeRangeValidationResponse(
    string CompanyCode,
    int RangeCount,
    int ActiveRangeCount,
    bool IsValid,
    int ErrorCount,
    int WarningCount,
    int IssueCount,
    IReadOnlyCollection<AccountCodeRangeValidationIssueResponse> Issues);

public sealed record AccountCodeRangeValidationIssueResponse(
    string Severity,
    string Code,
    string Message,
    string? RangeRole,
    string? RelatedRangeRole,
    string? RangeStart,
    string? RangeEnd);

public sealed record BootstrapStandardChartOfAccountsRequest(string? CompanyCode);

public sealed record BootstrapStandardChartOfAccountsResponse(
    string CompanyCode,
    int CreatedCount,
    int ReusedCount,
    IReadOnlyCollection<BootstrapStandardChartOfAccountsItemResponse> Accounts);

public sealed record BootstrapStandardChartOfAccountsItemResponse(
    Guid LedgerAccountId,
    string Role,
    string Action,
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    string Level,
    Guid? ParentAccountId,
    bool IsPostingAccount,
    string Status);

public sealed record ConfigureVoucherNumberingRuleRequest(
    string Prefix,
    int NumberPaddingWidth,
    bool IsActive = true);

public sealed record VoucherNumberingRuleResponse(
    string CompanyCode,
    string SourceType,
    string Prefix,
    int NumberPaddingWidth,
    bool IsActive,
    bool IsConfigured,
    DateTimeOffset? CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record ListVoucherNumberingRulesResponse(
    string CompanyCode,
    IReadOnlyCollection<VoucherNumberingRuleResponse> Rules);

public sealed record ConfigureAccountingControlSettingsRequest(
    string? CompanyCode,
    string BaseCurrencyCode,
    Guid? RetainedEarningsAccountId,
    Guid? IncomeSummaryAccountId,
    Guid? RoundingAccountId);

public sealed record ConfigureDefaultAccountingControlSettingsRequest(
    string? CompanyCode);

public sealed record AccountingControlSettingsResponse(
    string CompanyCode,
    string BaseCurrencyCode,
    Guid? RetainedEarningsAccountId,
    AccountingControlAccountResponse? RetainedEarningsAccount,
    Guid? IncomeSummaryAccountId,
    AccountingControlAccountResponse? IncomeSummaryAccount,
    Guid? RoundingAccountId,
    AccountingControlAccountResponse? RoundingAccount,
    bool IsConfigured,
    DateTimeOffset? CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record AccountingControlAccountResponse(
    Guid LedgerAccountId,
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    string Status);

public sealed record ConfigureOpeningBalanceProfileRequest(
    string? CompanyCode,
    DateOnly FiscalYearFrom,
    DateOnly FiscalYearTo,
    string Status,
    bool TransactionsAllowed,
    Guid? ProfitAndLossCarryForwardAccountId);

public sealed record OpeningBalanceProfileResponse(
    string CompanyCode,
    DateOnly FiscalYearFrom,
    DateOnly FiscalYearTo,
    string Status,
    bool TransactionsAllowed,
    Guid? ProfitAndLossCarryForwardAccountId,
    OpeningBalanceProfileAccountResponse? ProfitAndLossCarryForwardAccount,
    bool IsConfigured,
    DateTimeOffset? CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record OpeningBalanceProfileAccountResponse(
    Guid LedgerAccountId,
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    string Status);

public sealed record CreateAccountingPeriodRequest(
    string? CompanyCode,
    string? Name,
    DateOnly StartsOn,
    DateOnly EndsOn);

public sealed record ListAccountingPeriodsResponse(
    string CompanyCode,
    IReadOnlyCollection<AccountingPeriodResponse> Periods);

public sealed record AccountingPeriodResponse(
    Guid AccountingPeriodId,
    string CompanyCode,
    string Name,
    DateOnly StartsOn,
    DateOnly EndsOn,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    DateTimeOffset? ReopenedAtUtc,
    AccountingPeriodCloseArtifactResponse? CloseArtifact);

public sealed record AccountingPeriodCloseArtifactResponse(
    DateTimeOffset GeneratedAtUtc,
    string GeneratedBy,
    int CheckCount,
    int BlockedCheckCount,
    int CurrencyCount,
    int PostedJournalCount,
    int DraftJournalCount,
    IReadOnlyCollection<AccountingPeriodCloseReadinessCheckResponse> Checks,
    IReadOnlyCollection<AccountingPeriodCloseCurrencyResponse> Currencies,
    IReadOnlyCollection<AccountingPeriodCloseJournalArtifactResponse> CloseJournalEntries);

public sealed record AccountingPeriodCloseJournalArtifactResponse(
    Guid JournalEntryId,
    string SourceReference,
    string Memo,
    DateOnly EntryDate,
    string CurrencyCode,
    decimal TotalDebit,
    decimal TotalCredit);

public sealed record AccountingPeriodCloseReadinessResponse(
    AccountingPeriodResponse Period,
    bool CanClose,
    IReadOnlyCollection<AccountingPeriodCloseReadinessCheckResponse> Checks,
    IReadOnlyCollection<AccountingPeriodCloseCurrencyResponse> Currencies);

public sealed record AccountingPeriodCloseJournalPreviewResponse(
    AccountingPeriodResponse Period,
    string BaseCurrencyCode,
    bool CanGenerate,
    decimal NetIncome,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyCollection<string> Blockers,
    IReadOnlyCollection<AccountingCloseJournalPreviewEntryResponse> Entries);

public sealed record AccountingCloseJournalPreviewEntryResponse(
    string SourceReference,
    string Memo,
    DateOnly EntryDate,
    string CurrencyCode,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyCollection<AccountingCloseJournalPreviewLineResponse> Lines);

public sealed record AccountingCloseJournalPreviewLineResponse(
    Guid LedgerAccountId,
    string Code,
    string Name,
    string Type,
    decimal Debit,
    decimal Credit,
    string Description);

public sealed record AccountingPeriodCloseReadinessCheckResponse(
    string Code,
    string Status,
    string Message,
    string? Target);

public sealed record AccountingPeriodCloseCurrencyResponse(
    string CurrencyCode,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal Difference,
    int PostedJournalCount,
    int DraftJournalCount);

public sealed record ListJournalEntriesResponse(
    IReadOnlyCollection<JournalEntryRegisterItemResponse> Entries,
    int PageSize,
    bool HasMore,
    string? NextCursor,
    long FilteredCount);

public sealed record JournalEntryRegisterItemResponse(
    Guid JournalEntryId,
    DateOnly EntryDate,
    string CurrencyCode,
    string SourceType,
    string? SourceReference,
    string? Memo,
    string Status,
    decimal TotalDebit,
    decimal TotalCredit,
    int LineCount);

public sealed record JournalVoucherNumberPreviewResponse(
    string SourceType,
    DateOnly EntryDate,
    string Prefix,
    int SequenceYear,
    int NextSequence,
    int NumberPaddingWidth,
    string Reference);

public sealed record TrialBalanceResponse(
    DateOnly? FromDate,
    DateOnly AsOfDate,
    string CurrencyCode,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal TotalPeriodDebit,
    decimal TotalPeriodCredit,
    decimal Difference,
    IReadOnlyCollection<TrialBalanceLineResponse> Lines);

public sealed record TrialBalanceLineResponse(
    Guid LedgerAccountId,
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    decimal OpeningBalance,
    decimal PeriodDebit,
    decimal PeriodCredit,
    decimal DebitBalance,
    decimal CreditBalance,
    decimal NetBalance,
    int ActivityCount);

public sealed record ProfitAndLossStatementResponse(
    DateOnly? FromDate,
    DateOnly ToDate,
    string CurrencyCode,
    decimal TotalRevenue,
    decimal TotalExpense,
    decimal NetIncome,
    IReadOnlyCollection<ProfitAndLossStatementSectionResponse> Sections);

public sealed record ProfitAndLossStatementSectionResponse(
    string Type,
    string Title,
    decimal Total,
    IReadOnlyCollection<ProfitAndLossStatementLineResponse> Lines);

public sealed record ProfitAndLossStatementLineResponse(
    Guid LedgerAccountId,
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    decimal Debit,
    decimal Credit,
    decimal Amount,
    int ActivityCount);

public sealed record BalanceSheetResponse(
    DateOnly AsOfDate,
    string CurrencyCode,
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal TotalEquity,
    decimal TotalLiabilitiesAndEquity,
    decimal Difference,
    IReadOnlyCollection<BalanceSheetSectionResponse> Sections);

public sealed record BalanceSheetSectionResponse(
    string Type,
    string Title,
    decimal Total,
    IReadOnlyCollection<BalanceSheetLineResponse> Lines);

public sealed record BalanceSheetLineResponse(
    Guid? LedgerAccountId,
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    decimal Debit,
    decimal Credit,
    decimal Amount,
    int ActivityCount,
    bool IsSystemLine);

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

public sealed record PreviewOpeningBalanceImportRequest(
    DateOnly EntryDate,
    string CurrencyCode,
    string? SourceReference,
    string? Memo,
    DateOnly ProfileFromDate,
    DateOnly ProfileToDate,
    string ProfileStatus,
    bool TransactionsAllowed,
    Guid? ProfitAndLossCarryForwardAccountId,
    IReadOnlyCollection<PreviewOpeningBalanceImportLineRequest> Lines);

public sealed record PreviewOpeningBalanceImportLineRequest(
    string AccountCode,
    decimal Debit,
    decimal Credit,
    string? Description);

public sealed record PreviewOpeningBalanceImportTextRequest(
    DateOnly EntryDate,
    string CurrencyCode,
    string? SourceReference,
    string? Memo,
    DateOnly ProfileFromDate,
    DateOnly ProfileToDate,
    string ProfileStatus,
    bool TransactionsAllowed,
    Guid? ProfitAndLossCarryForwardAccountId,
    string ImportText,
    string? Delimiter);

public sealed record PostOpeningBalanceImportRequest(
    DateOnly EntryDate,
    string CurrencyCode,
    string? SourceReference,
    string? Memo,
    DateOnly ProfileFromDate,
    DateOnly ProfileToDate,
    string ProfileStatus,
    bool TransactionsAllowed,
    Guid? ProfitAndLossCarryForwardAccountId,
    IReadOnlyCollection<PostOpeningBalanceImportLineRequest> Lines);

public sealed record PostOpeningBalanceImportLineRequest(
    string AccountCode,
    decimal Debit,
    decimal Credit,
    string? Description);

public sealed record PreviewOpeningBalanceImportResponse(
    DateOnly EntryDate,
    string CurrencyCode,
    string SourceReference,
    string? Memo,
    bool CanPost,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal Difference,
    int ImportedLineCount,
    int ValidLineCount,
    int InvalidLineCount,
    IReadOnlyCollection<string> Blockers,
    IReadOnlyCollection<PreviewOpeningBalanceImportLineResponse> Lines);

public sealed record PreviewOpeningBalanceImportLineResponse(
    int LineNumber,
    string AccountCode,
    Guid? LedgerAccountId,
    string? LedgerAccountName,
    string? AccountType,
    string? NormalBalance,
    decimal Debit,
    decimal Credit,
    string? Description,
    bool IsValid,
    IReadOnlyCollection<string> Issues);

public sealed record PreviewOpeningBalanceImportTextResponse(
    string Format,
    int ParsedLineCount,
    int IgnoredLineCount,
    IReadOnlyCollection<OpeningBalanceImportTextParseIssueResponse> ParseIssues,
    PreviewOpeningBalanceImportResponse Preview);

public sealed record OpeningBalanceImportTextParseIssueResponse(
    int LineNumber,
    string Column,
    string Message,
    string? RawValue);

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

public sealed record JournalEntrySourceDocumentResponse(
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
    DateOnly? FiscalYearFrom,
    DateOnly? FiscalYearTo,
    bool? TransactionsAllowed,
    Guid? ProfitAndLossCarryForwardAccountId,
    string? ProfitAndLossCarryForwardAccountCode,
    string? ProfitAndLossCarryForwardAccountName,
    string? Message);

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
    decimal OpeningBalance,
    decimal PeriodDebit,
    decimal PeriodCredit,
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
