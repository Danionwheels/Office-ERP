using SafarSuite.ControlDesk.Api.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.CreateLedgerAccount;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountActivity;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListJournalEntries;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Accounting;

namespace SafarSuite.ControlDesk.Api.Modules.Accounting;

public static class AccountingEndpoints
{
    public static IEndpointRouteBuilder MapAccountingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/accounting")
            .WithTags("Accounting");

        group.MapPost("/ledger-accounts", CreateLedgerAccountAsync);
        group.MapGet("/journal-entries", ListJournalEntriesAsync);
        group.MapGet("/ledger-accounts/{ledgerAccountId:guid}/activity", GetLedgerAccountActivityAsync);

        return endpoints;
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
            result.Value.IsPostingAccount,
            result.Value.Status);

        return Results.Created($"/api/v1/accounting/ledger-accounts/{response.LedgerAccountId}", response);
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
}
