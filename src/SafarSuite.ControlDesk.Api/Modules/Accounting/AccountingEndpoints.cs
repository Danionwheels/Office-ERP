using SafarSuite.ControlDesk.Api.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureAccountCodeRange;
using SafarSuite.ControlDesk.Application.Modules.Accounting.CreateLedgerAccount;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountActivity;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListAccountCodeRanges;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListJournalEntries;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListLedgerAccounts;
using SafarSuite.ControlDesk.Application.Modules.Accounting.PostManualJournalEntry;
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
        group.MapPost("/ledger-accounts", CreateLedgerAccountAsync);
        group.MapPut("/ledger-accounts/{ledgerAccountId:guid}", UpdateLedgerAccountAsync);
        group.MapGet("/ledger-accounts/suggest-code", SuggestLedgerAccountCodeAsync);
        group.MapGet("/accounting-setup/account-code-ranges", ListAccountCodeRangesAsync);
        group.MapPut("/accounting-setup/account-code-ranges/{role}", ConfigureAccountCodeRangeAsync);
        group.MapGet("/journal-entries", ListJournalEntriesAsync);
        group.MapPost("/journal-entries/manual", PostManualJournalEntryAsync);
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
            request.IsPostingAccount);

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
            result.Value.ParentAccountId,
            result.Value.IsPostingAccount,
            result.Value.Status);

        return Results.Created($"/api/v1/accounting/ledger-accounts/{response.LedgerAccountId}", response);
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
        GetLedgerAccountActivityHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetLedgerAccountActivityQuery(ledgerAccountId, fromDate, toDate),
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
}
