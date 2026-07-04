using SafarSuite.ControlDesk.Api.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.CloseAccountingPeriod;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureAccountingControlSettings;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureAccountCodeRange;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureDefaultAccountingControlSettings;
using SafarSuite.ControlDesk.Application.Modules.Accounting.CreateAccountingPeriod;
using SafarSuite.ControlDesk.Application.Modules.Accounting.CreateLedgerAccount;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountingControlSettings;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetBalanceSheet;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetJournalEntry;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetJournalEntrySourceDocument;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountReconciliation;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountRepairPlan;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountActivity;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountingPeriodCloseJournalPreview;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountingPeriodCloseReadiness;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetProfitAndLossStatement;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetTrialBalance;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListAccountCodeRanges;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListAccountingPeriods;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListJournalEntries;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListLedgerAccounts;
using SafarSuite.ControlDesk.Application.Modules.Accounting.PostManualJournalEntry;
using SafarSuite.ControlDesk.Application.Modules.Accounting.PostOpeningBalanceImport;
using SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewJournalVoucherNumber;
using SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewOpeningBalanceImport;
using SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewOpeningBalanceImportText;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ReopenAccountingPeriod;
using SafarSuite.ControlDesk.Application.Modules.Accounting.SuggestLedgerAccountCode;
using SafarSuite.ControlDesk.Application.Modules.Accounting.UpdateLedgerAccount;
using SafarSuite.ControlDesk.Application.Modules.Accounting.VoidManualJournalEntry;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Accounting;

namespace SafarSuite.ControlDesk.Api.Modules.Accounting;

public static class AccountingEndpoints
{
    public static IEndpointRouteBuilder MapAccountingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/accounting")
            .WithTags("Accounting");

        group.MapGet("/ledger-accounts", ListLedgerAccountsAsync);
        group.MapGet("/ledger-accounts/reconciliation", GetLedgerAccountReconciliationAsync);
        group.MapGet("/ledger-accounts/repair-plan", GetLedgerAccountRepairPlanAsync);
        group.MapPost("/ledger-accounts", CreateLedgerAccountAsync);
        group.MapPut("/ledger-accounts/{ledgerAccountId:guid}", UpdateLedgerAccountAsync);
        group.MapGet("/ledger-accounts/suggest-code", SuggestLedgerAccountCodeAsync);
        group.MapGet("/accounting-setup/account-code-ranges", ListAccountCodeRangesAsync);
        group.MapPut("/accounting-setup/account-code-ranges/{role}", ConfigureAccountCodeRangeAsync);
        group.MapGet("/accounting-controls", GetAccountingControlSettingsAsync);
        group.MapPut("/accounting-controls", ConfigureAccountingControlSettingsAsync);
        group.MapPost("/accounting-controls/defaults", ConfigureDefaultAccountingControlSettingsAsync);
        group.MapGet("/accounting-periods", ListAccountingPeriodsAsync);
        group.MapPost("/accounting-periods", CreateAccountingPeriodAsync);
        group.MapGet("/accounting-periods/{accountingPeriodId:guid}/close-readiness", GetAccountingPeriodCloseReadinessAsync);
        group.MapGet("/accounting-periods/{accountingPeriodId:guid}/close-journal-preview", GetAccountingPeriodCloseJournalPreviewAsync);
        group.MapPost("/accounting-periods/{accountingPeriodId:guid}/close", CloseAccountingPeriodAsync);
        group.MapPost("/accounting-periods/{accountingPeriodId:guid}/reopen", ReopenAccountingPeriodAsync);
        group.MapGet("/journal-entries", ListJournalEntriesAsync);
        group.MapGet("/journal-entries/voucher-number-preview", PreviewJournalVoucherNumberAsync);
        group.MapGet("/journal-entries/{journalEntryId:guid}", GetJournalEntryAsync);
        group.MapGet("/journal-entries/{journalEntryId:guid}/source-document", GetJournalEntrySourceDocumentAsync);
        group.MapGet("/trial-balance", GetTrialBalanceAsync);
        group.MapGet("/profit-and-loss", GetProfitAndLossStatementAsync);
        group.MapGet("/balance-sheet", GetBalanceSheetAsync);
        group.MapPost("/journal-entries/manual", PostManualJournalEntryAsync);
        group.MapPost("/journal-entries/opening-balances/preview", PreviewOpeningBalanceImportAsync);
        group.MapPost("/journal-entries/opening-balances/text-preview", PreviewOpeningBalanceImportTextAsync);
        group.MapPost("/journal-entries/opening-balances", PostOpeningBalanceImportAsync);
        group.MapPost("/journal-entries/{journalEntryId:guid}/void", VoidManualJournalEntryAsync);
        group.MapGet("/ledger-accounts/{ledgerAccountId:guid}/activity", GetLedgerAccountActivityAsync);

        return endpoints;
    }

    private static async Task<IResult> ListLedgerAccountsAsync(
        string? companyCode,
        string? search,
        string? type,
        string? status,
        bool? isPostingAccount,
        string? role,
        ListLedgerAccountsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListLedgerAccountsQuery(
                companyCode,
                search,
                type,
                status,
                isPostingAccount,
                role),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new ListLedgerAccountsResponse(
            result.Value.CompanyCode,
            result.Value.Accounts.Select(account => new LedgerAccountSummaryResponse(
                account.LedgerAccountId,
                account.Code,
                account.DisplayCode,
                account.Name,
                account.Type,
                account.NormalBalance,
                account.Level,
                account.ParentAccountId,
                account.IsPostingAccount,
                account.Status,
                account.CreatedAtUtc,
                account.RangeRole,
                account.RangeDisplayName)).ToArray()));
    }

    private static async Task<IResult> CreateLedgerAccountAsync(
        CreateLedgerAccountRequest request,
        CreateLedgerAccountHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateLedgerAccountCommand(
            request.Code,
            request.Name,
            request.Type,
            request.NormalBalance,
            request.ParentAccountId,
            request.IsPostingAccount,
            request.Level);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new CreateLedgerAccountResponse(
            result.Value.LedgerAccountId,
            result.Value.Code,
            result.Value.Name,
            result.Value.Type,
            result.Value.NormalBalance,
            result.Value.Level,
            result.Value.ParentAccountId,
            result.Value.IsPostingAccount,
            result.Value.Status);

        return Results.Created($"/api/v1/accounting/ledger-accounts/{response.LedgerAccountId}", response);
    }

    private static async Task<IResult> GetLedgerAccountReconciliationAsync(
        string? companyCode,
        GetLedgerAccountReconciliationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetLedgerAccountReconciliationQuery(companyCode),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new LedgerAccountReconciliationResponse(
            result.Value.CompanyCode,
            result.Value.AccountCount,
            result.Value.IssueCount,
            result.Value.Items.Select(item => new LedgerAccountReconciliationItemResponse(
                item.LedgerAccountId,
                item.Code,
                item.DisplayCode,
                item.Name,
                item.Type,
                item.NormalBalance,
                item.Level,
                item.ParentAccountId,
                item.IsPostingAccount,
                item.Status,
                item.RangeRole,
                item.RangeDisplayName,
                item.Issues.Select(issue => new LedgerAccountReconciliationIssueResponse(
                    issue.Severity,
                    issue.Code,
                    issue.Message)).ToArray())).ToArray()));
    }

    private static async Task<IResult> GetLedgerAccountRepairPlanAsync(
        string? companyCode,
        GetLedgerAccountRepairPlanHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetLedgerAccountRepairPlanQuery(companyCode),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new LedgerAccountRepairPlanResponse(
            result.Value.CompanyCode,
            result.Value.AccountCount,
            result.Value.IssueCount,
            result.Value.ActionCount,
            result.Value.Items.Select(item => new LedgerAccountRepairPlanItemResponse(
                item.LedgerAccountId,
                item.Code,
                item.DisplayCode,
                item.Name,
                item.Type,
                item.NormalBalance,
                item.Level,
                item.ParentAccountId,
                item.IsPostingAccount,
                item.Status,
                item.RangeRole,
                item.RangeDisplayName,
                item.Actions.Select(action => new LedgerAccountRepairActionResponse(
                    action.IssueCode,
                    action.Severity,
                    action.ActionCode,
                    action.Title,
                    action.Description,
                    action.RepairMode,
                    action.IsAutomatable,
                    action.CurrentValue,
                    action.SuggestedValue,
                    action.Notes)).ToArray())).ToArray()));
    }

    private static async Task<IResult> UpdateLedgerAccountAsync(
        Guid ledgerAccountId,
        UpdateLedgerAccountRequest request,
        UpdateLedgerAccountHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new UpdateLedgerAccountCommand(
                ledgerAccountId,
                request.Name,
                request.IsPostingAccount,
                request.Status),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new UpdateLedgerAccountResponse(
            result.Value.LedgerAccountId,
            result.Value.Code,
            result.Value.Name,
            result.Value.Type,
            result.Value.NormalBalance,
            result.Value.Level,
            result.Value.ParentAccountId,
            result.Value.IsPostingAccount,
            result.Value.Status,
            result.Value.CreatedAtUtc));
    }

    private static async Task<IResult> SuggestLedgerAccountCodeAsync(
        string role,
        string? companyCode,
        SuggestLedgerAccountCodeHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new SuggestLedgerAccountCodeQuery(role, companyCode),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new SuggestLedgerAccountCodeResponse(
            result.Value.CompanyCode,
            result.Value.Role,
            result.Value.SuggestedCode,
            result.Value.DisplayCode,
            result.Value.Type,
            result.Value.NormalBalance,
            result.Value.IsPostingAccount,
            result.Value.RangeStart,
            result.Value.RangeEnd,
            result.Value.ParentCode));
    }

    private static async Task<IResult> ListAccountCodeRangesAsync(
        string? companyCode,
        ListAccountCodeRangesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListAccountCodeRangesQuery(companyCode),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new ListAccountCodeRangesResponse(
            result.Value.CompanyCode,
            result.Value.Ranges.Select(ToResponse).ToArray()));
    }

    private static async Task<IResult> ConfigureAccountCodeRangeAsync(
        string role,
        string? companyCode,
        ConfigureAccountCodeRangeRequest request,
        ConfigureAccountCodeRangeHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ConfigureAccountCodeRangeCommand(
                companyCode,
                role,
                request.DisplayName,
                request.SearchPrefix,
                request.RangeStart,
                request.RangeEnd,
                request.CodeLength,
                request.AccountType,
                request.NormalBalance,
                request.IsPostingAccount,
                request.ParentCode,
                request.IsActive),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(ToResponse(result.Value));
    }

    private static async Task<IResult> GetAccountingControlSettingsAsync(
        string? companyCode,
        GetAccountingControlSettingsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetAccountingControlSettingsQuery(companyCode),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(ToResponse(result.Value));
    }

    private static async Task<IResult> ConfigureAccountingControlSettingsAsync(
        ConfigureAccountingControlSettingsRequest request,
        ConfigureAccountingControlSettingsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ConfigureAccountingControlSettingsCommand(
                request.CompanyCode,
                request.BaseCurrencyCode,
                request.RetainedEarningsAccountId,
                request.IncomeSummaryAccountId,
                request.RoundingAccountId),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(ToResponse(result.Value));
    }

    private static async Task<IResult> ConfigureDefaultAccountingControlSettingsAsync(
        ConfigureDefaultAccountingControlSettingsRequest request,
        ConfigureDefaultAccountingControlSettingsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ConfigureDefaultAccountingControlSettingsCommand(request.CompanyCode),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(ToResponse(result.Value));
    }

    private static async Task<IResult> ListAccountingPeriodsAsync(
        string? companyCode,
        DateOnly? fromDate,
        DateOnly? toDate,
        ListAccountingPeriodsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListAccountingPeriodsQuery(companyCode, fromDate, toDate),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new ListAccountingPeriodsResponse(
            result.Value.CompanyCode,
            result.Value.Periods.Select(ToResponse).ToArray()));
    }

    private static async Task<IResult> CreateAccountingPeriodAsync(
        CreateAccountingPeriodRequest request,
        CreateAccountingPeriodHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateAccountingPeriodCommand(
                request.CompanyCode,
                request.Name,
                request.StartsOn,
                request.EndsOn),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = ToResponse(result.Value);

        return Results.Created($"/api/v1/accounting/accounting-periods/{response.AccountingPeriodId}", response);
    }

    private static async Task<IResult> GetAccountingPeriodCloseReadinessAsync(
        Guid accountingPeriodId,
        GetAccountingPeriodCloseReadinessHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetAccountingPeriodCloseReadinessQuery(accountingPeriodId),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new AccountingPeriodCloseReadinessResponse(
            ToResponse(result.Value.Period),
            result.Value.CanClose,
            result.Value.Checks.Select(check => new AccountingPeriodCloseReadinessCheckResponse(
                check.Code,
                check.Status,
                check.Message,
                check.Target)).ToArray(),
            result.Value.Currencies.Select(currency => new AccountingPeriodCloseCurrencyResponse(
                currency.CurrencyCode,
                currency.TotalDebit,
                currency.TotalCredit,
                currency.Difference,
                currency.PostedJournalCount,
                currency.DraftJournalCount)).ToArray()));
    }

    private static async Task<IResult> CloseAccountingPeriodAsync(
        Guid accountingPeriodId,
        HttpContext httpContext,
        CloseAccountingPeriodHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CloseAccountingPeriodCommand(accountingPeriodId, ResolveActor(httpContext)),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(ToResponse(result.Value));
    }

    private static async Task<IResult> GetAccountingPeriodCloseJournalPreviewAsync(
        Guid accountingPeriodId,
        GetAccountingPeriodCloseJournalPreviewHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetAccountingPeriodCloseJournalPreviewQuery(accountingPeriodId),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new AccountingPeriodCloseJournalPreviewResponse(
            ToResponse(result.Value.Period),
            result.Value.BaseCurrencyCode,
            result.Value.CanGenerate,
            result.Value.NetIncome,
            result.Value.TotalDebit,
            result.Value.TotalCredit,
            result.Value.Blockers,
            result.Value.Entries.Select(entry => new AccountingCloseJournalPreviewEntryResponse(
                entry.SourceReference,
                entry.Memo,
                entry.EntryDate,
                entry.CurrencyCode,
                entry.TotalDebit,
                entry.TotalCredit,
                entry.Lines.Select(line => new AccountingCloseJournalPreviewLineResponse(
                    line.LedgerAccountId,
                    line.Code,
                    line.Name,
                    line.Type,
                    line.Debit,
                    line.Credit,
                    line.Description)).ToArray())).ToArray()));
    }

    private static async Task<IResult> ReopenAccountingPeriodAsync(
        Guid accountingPeriodId,
        ReopenAccountingPeriodHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ReopenAccountingPeriodCommand(accountingPeriodId),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(ToResponse(result.Value));
    }

    private static async Task<IResult> ListJournalEntriesAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        string? sourceType,
        ListJournalEntriesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListJournalEntriesQuery(fromDate, toDate, sourceType),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new ListJournalEntriesResponse(
            result.Value.Entries.Select(entry => new JournalEntrySummaryResponse(
                entry.JournalEntryId,
                entry.EntryDate,
                entry.CurrencyCode,
                entry.SourceType,
                entry.SourceReference,
                entry.Memo,
                entry.Status,
                entry.TotalDebit,
                entry.TotalCredit,
                entry.Lines.Select(line => new JournalEntryLineResponse(
                    line.LedgerAccountId,
                    line.Debit,
                    line.Credit,
                    line.Description)).ToArray())).ToArray());

        return Results.Ok(response);
    }

    private static async Task<IResult> GetJournalEntryAsync(
        Guid journalEntryId,
        GetJournalEntryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetJournalEntryQuery(journalEntryId),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(ToResponse(result.Value));
    }

    private static async Task<IResult> PreviewJournalVoucherNumberAsync(
        string sourceType,
        DateOnly entryDate,
        PreviewJournalVoucherNumberHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new PreviewJournalVoucherNumberQuery(sourceType, entryDate),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new JournalVoucherNumberPreviewResponse(
            result.Value.SourceType,
            result.Value.EntryDate,
            result.Value.Prefix,
            result.Value.SequenceYear,
            result.Value.NextSequence,
            result.Value.Reference));
    }

    private static async Task<IResult> GetJournalEntrySourceDocumentAsync(
        Guid journalEntryId,
        GetJournalEntrySourceDocumentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetJournalEntrySourceDocumentQuery(journalEntryId),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(ToResponse(result.Value));
    }

    private static async Task<IResult> GetTrialBalanceAsync(
        DateOnly? fromDate,
        DateOnly? asOfDate,
        string? currencyCode,
        GetTrialBalanceHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetTrialBalanceQuery(fromDate, asOfDate, currencyCode),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new TrialBalanceResponse(
            result.Value.FromDate,
            result.Value.AsOfDate,
            result.Value.CurrencyCode,
            result.Value.TotalDebit,
            result.Value.TotalCredit,
            result.Value.TotalPeriodDebit,
            result.Value.TotalPeriodCredit,
            result.Value.Difference,
            result.Value.Lines.Select(line => new TrialBalanceLineResponse(
                line.LedgerAccountId,
                line.Code,
                line.Name,
                line.Type,
                line.NormalBalance,
                line.OpeningBalance,
                line.PeriodDebit,
                line.PeriodCredit,
                line.DebitBalance,
                line.CreditBalance,
                line.NetBalance,
                line.ActivityCount)).ToArray()));
    }

    private static async Task<IResult> GetProfitAndLossStatementAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        string? currencyCode,
        GetProfitAndLossStatementHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetProfitAndLossStatementQuery(fromDate, toDate, currencyCode),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new ProfitAndLossStatementResponse(
            result.Value.FromDate,
            result.Value.ToDate,
            result.Value.CurrencyCode,
            result.Value.TotalRevenue,
            result.Value.TotalExpense,
            result.Value.NetIncome,
            result.Value.Sections.Select(section => new ProfitAndLossStatementSectionResponse(
                section.Type,
                section.Title,
                section.Total,
                section.Lines.Select(line => new ProfitAndLossStatementLineResponse(
                    line.LedgerAccountId,
                    line.Code,
                    line.Name,
                    line.Type,
                    line.NormalBalance,
                    line.Debit,
                    line.Credit,
                    line.Amount,
                    line.ActivityCount)).ToArray())).ToArray()));
    }

    private static async Task<IResult> GetBalanceSheetAsync(
        DateOnly? asOfDate,
        string? currencyCode,
        GetBalanceSheetHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetBalanceSheetQuery(asOfDate, currencyCode),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new BalanceSheetResponse(
            result.Value.AsOfDate,
            result.Value.CurrencyCode,
            result.Value.TotalAssets,
            result.Value.TotalLiabilities,
            result.Value.TotalEquity,
            result.Value.TotalLiabilitiesAndEquity,
            result.Value.Difference,
            result.Value.Sections.Select(section => new BalanceSheetSectionResponse(
                section.Type,
                section.Title,
                section.Total,
                section.Lines.Select(line => new BalanceSheetLineResponse(
                    line.LedgerAccountId,
                    line.Code,
                    line.Name,
                    line.Type,
                    line.NormalBalance,
                    line.Debit,
                    line.Credit,
                    line.Amount,
                    line.ActivityCount,
                    line.IsSystemLine)).ToArray())).ToArray()));
    }

    private static async Task<IResult> PostManualJournalEntryAsync(
        PostManualJournalEntryRequest request,
        PostManualJournalEntryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new PostManualJournalEntryCommand(
                request.EntryDate,
                request.CurrencyCode,
                request.SourceReference,
                request.Memo,
                request.Lines?.Select(line => new PostManualJournalEntryLineCommand(
                    line.LedgerAccountId,
                    line.Debit,
                    line.Credit,
                    line.Description)).ToArray()!),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = ToResponse(result.Value);

        return Results.Created($"/api/v1/accounting/journal-entries/{response.JournalEntryId}", response);
    }

    private static async Task<IResult> PreviewOpeningBalanceImportAsync(
        PreviewOpeningBalanceImportRequest request,
        PreviewOpeningBalanceImportHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new PreviewOpeningBalanceImportCommand(
                request.EntryDate,
                request.CurrencyCode,
                request.SourceReference,
                request.Memo,
                request.Lines?.Select(line => new PreviewOpeningBalanceImportLineCommand(
                    line.AccountCode,
                    line.Debit,
                    line.Credit,
                    line.Description)).ToArray() ?? []),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new PreviewOpeningBalanceImportResponse(
            result.Value.EntryDate,
            result.Value.CurrencyCode,
            result.Value.SourceReference,
            result.Value.Memo,
            result.Value.CanPost,
            result.Value.TotalDebit,
            result.Value.TotalCredit,
            result.Value.Difference,
            result.Value.ImportedLineCount,
            result.Value.ValidLineCount,
            result.Value.InvalidLineCount,
            result.Value.Blockers,
            result.Value.Lines.Select(line => new PreviewOpeningBalanceImportLineResponse(
                line.LineNumber,
                line.AccountCode,
                line.LedgerAccountId,
                line.LedgerAccountName,
                line.AccountType,
                line.NormalBalance,
                line.Debit,
                line.Credit,
                line.Description,
                line.IsValid,
                line.Issues)).ToArray()));
    }

    private static async Task<IResult> PostOpeningBalanceImportAsync(
        PostOpeningBalanceImportRequest request,
        PostOpeningBalanceImportHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new PostOpeningBalanceImportCommand(
                request.EntryDate,
                request.CurrencyCode,
                request.SourceReference,
                request.Memo,
                request.Lines?.Select(line => new PostOpeningBalanceImportLineCommand(
                    line.AccountCode,
                    line.Debit,
                    line.Credit,
                    line.Description)).ToArray() ?? []),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = ToResponse(result.Value);

        return Results.Created($"/api/v1/accounting/journal-entries/{response.JournalEntryId}", response);
    }

    private static async Task<IResult> PreviewOpeningBalanceImportTextAsync(
        PreviewOpeningBalanceImportTextRequest request,
        PreviewOpeningBalanceImportTextHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new PreviewOpeningBalanceImportTextCommand(
                request.EntryDate,
                request.CurrencyCode,
                request.SourceReference,
                request.Memo,
                request.ImportText,
                request.Delimiter),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new PreviewOpeningBalanceImportTextResponse(
            result.Value.Format,
            result.Value.ParsedLineCount,
            result.Value.IgnoredLineCount,
            result.Value.ParseIssues.Select(issue => new OpeningBalanceImportTextParseIssueResponse(
                issue.LineNumber,
                issue.Column,
                issue.Message,
                issue.RawValue)).ToArray(),
            ToResponse(result.Value.Preview)));
    }

    private static async Task<IResult> VoidManualJournalEntryAsync(
        Guid journalEntryId,
        VoidManualJournalEntryRequest request,
        VoidManualJournalEntryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new VoidManualJournalEntryCommand(
                journalEntryId,
                request.VoidDate,
                request.Reason),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        return Results.Ok(new VoidManualJournalEntryResponse(
            result.Value.OriginalJournalEntryId,
            result.Value.ReversalJournalEntryId,
            result.Value.OriginalJournalEntryStatus,
            result.Value.ReversalJournalEntryStatus,
            result.Value.VoidDate,
            result.Value.TotalDebit,
            result.Value.TotalCredit,
            result.Value.CurrencyCode,
            result.Value.Lines.Select(line => new JournalEntryLineResponse(
                line.LedgerAccountId,
                line.Debit,
                line.Credit,
                line.Description)).ToArray()));
    }

    private static async Task<IResult> GetLedgerAccountActivityAsync(
        Guid ledgerAccountId,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? currencyCode,
        GetLedgerAccountActivityHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetLedgerAccountActivityQuery(ledgerAccountId, fromDate, toDate, currencyCode),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new LedgerAccountActivityResponse(
            result.Value.LedgerAccountId,
            result.Value.Code,
            result.Value.Name,
            result.Value.Type,
            result.Value.NormalBalance,
            result.Value.FromDate,
            result.Value.ToDate,
            result.Value.CurrencyCode,
            result.Value.OpeningBalance,
            result.Value.PeriodDebit,
            result.Value.PeriodCredit,
            result.Value.EndingBalance,
            result.Value.Lines.Select(line => new LedgerAccountActivityLineResponse(
                line.JournalEntryId,
                line.EntryDate,
                line.SourceType,
                line.SourceReference,
                line.Memo,
                line.Status,
                line.Debit,
                line.Credit,
                line.RunningBalance,
                line.CurrencyCode,
                line.Description)).ToArray());

        return Results.Ok(response);
    }

    private static JournalEntrySummaryResponse ToResponse(PostManualJournalEntryResult entry)
    {
        return new JournalEntrySummaryResponse(
            entry.JournalEntryId,
            entry.EntryDate,
            entry.CurrencyCode,
            entry.SourceType,
            entry.SourceReference,
            entry.Memo,
            entry.Status,
            entry.TotalDebit,
            entry.TotalCredit,
            entry.Lines.Select(line => new JournalEntryLineResponse(
                line.LedgerAccountId,
                line.Debit,
                line.Credit,
                line.Description)).ToArray());
    }

    private static JournalEntrySummaryResponse ToResponse(JournalEntrySummaryResult entry)
    {
        return new JournalEntrySummaryResponse(
            entry.JournalEntryId,
            entry.EntryDate,
            entry.CurrencyCode,
            entry.SourceType,
            entry.SourceReference,
            entry.Memo,
            entry.Status,
            entry.TotalDebit,
            entry.TotalCredit,
            entry.Lines.Select(line => new JournalEntryLineResponse(
                line.LedgerAccountId,
                line.Debit,
                line.Credit,
                line.Description)).ToArray());
    }

    private static PreviewOpeningBalanceImportResponse ToResponse(PreviewOpeningBalanceImportResult preview)
    {
        return new PreviewOpeningBalanceImportResponse(
            preview.EntryDate,
            preview.CurrencyCode,
            preview.SourceReference,
            preview.Memo,
            preview.CanPost,
            preview.TotalDebit,
            preview.TotalCredit,
            preview.Difference,
            preview.ImportedLineCount,
            preview.ValidLineCount,
            preview.InvalidLineCount,
            preview.Blockers,
            preview.Lines.Select(line => new PreviewOpeningBalanceImportLineResponse(
                line.LineNumber,
                line.AccountCode,
                line.LedgerAccountId,
                line.LedgerAccountName,
                line.AccountType,
                line.NormalBalance,
                line.Debit,
                line.Credit,
                line.Description,
                line.IsValid,
                line.Issues)).ToArray());
    }

    private static JournalEntrySourceDocumentResponse ToResponse(JournalEntrySourceDocumentResult sourceDocument)
    {
        return new JournalEntrySourceDocumentResponse(
            sourceDocument.JournalEntryId,
            sourceDocument.SourceType,
            sourceDocument.SourceReference,
            sourceDocument.IsResolved,
            sourceDocument.DocumentKind,
            sourceDocument.DocumentId,
            sourceDocument.ClientId,
            sourceDocument.RelatedInvoiceId,
            sourceDocument.Reference,
            sourceDocument.Status,
            sourceDocument.DocumentDate,
            sourceDocument.CurrencyCode,
            sourceDocument.Amount,
            sourceDocument.Label,
            sourceDocument.DashboardModule,
            sourceDocument.DashboardStep,
            sourceDocument.Message);
    }

    private static AccountCodeRangeResponse ToResponse(AccountCodeRangeResult range)
    {
        return new AccountCodeRangeResponse(
            range.AccountCodeRangeId,
            range.CompanyCode,
            range.Role,
            range.DisplayName,
            range.SearchPrefix,
            range.RangeStart,
            range.RangeEnd,
            range.CodeLength,
            range.AccountType,
            range.NormalBalance,
            range.IsPostingAccount,
            range.ParentCode,
            range.IsActive);
    }

    private static AccountingControlSettingsResponse ToResponse(GetAccountingControlSettingsResult settings)
    {
        return new AccountingControlSettingsResponse(
            settings.CompanyCode,
            settings.BaseCurrencyCode,
            settings.RetainedEarningsAccountId,
            ToResponse(settings.RetainedEarningsAccount),
            settings.IncomeSummaryAccountId,
            ToResponse(settings.IncomeSummaryAccount),
            settings.RoundingAccountId,
            ToResponse(settings.RoundingAccount),
            settings.IsConfigured,
            settings.CreatedAtUtc,
            settings.UpdatedAtUtc);
    }

    private static AccountingControlAccountResponse? ToResponse(AccountingControlAccountResult? account)
    {
        return account is null
            ? null
            : new AccountingControlAccountResponse(
                account.LedgerAccountId,
                account.Code,
                account.Name,
                account.Type,
                account.NormalBalance,
                account.Status);
    }

    private static AccountingPeriodResponse ToResponse(AccountingPeriodResult period)
    {
        return new AccountingPeriodResponse(
            period.AccountingPeriodId,
            period.CompanyCode,
            period.Name,
            period.StartsOn,
            period.EndsOn,
            period.Status,
            period.CreatedAtUtc,
            period.UpdatedAtUtc,
            period.ClosedAtUtc,
            period.ReopenedAtUtc,
            period.CloseArtifact is null
                ? null
                : new AccountingPeriodCloseArtifactResponse(
                    period.CloseArtifact.GeneratedAtUtc,
                    period.CloseArtifact.GeneratedBy,
                    period.CloseArtifact.CheckCount,
                    period.CloseArtifact.BlockedCheckCount,
                    period.CloseArtifact.CurrencyCount,
                    period.CloseArtifact.PostedJournalCount,
                    period.CloseArtifact.DraftJournalCount,
                    period.CloseArtifact.Checks.Select(check => new AccountingPeriodCloseReadinessCheckResponse(
                        check.Code,
                        check.Status,
                        check.Message,
                        check.Target)).ToArray(),
                    period.CloseArtifact.Currencies.Select(currency => new AccountingPeriodCloseCurrencyResponse(
                        currency.CurrencyCode,
                        currency.TotalDebit,
                        currency.TotalCredit,
                        currency.Difference,
                        currency.PostedJournalCount,
                        currency.DraftJournalCount)).ToArray(),
                    period.CloseArtifact.CloseJournalEntries.Select(entry =>
                        new AccountingPeriodCloseJournalArtifactResponse(
                            entry.JournalEntryId,
                            entry.SourceReference,
                            entry.Memo,
                            entry.EntryDate,
                            entry.CurrencyCode,
                            entry.TotalDebit,
                            entry.TotalCredit)).ToArray()));
    }

    private static string? ResolveActor(HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            return httpContext.User.Identity.Name;
        }

        return httpContext.Request.Headers.TryGetValue("X-Safar-Actor", out var actor)
            ? actor.FirstOrDefault()
            : null;
    }
}
