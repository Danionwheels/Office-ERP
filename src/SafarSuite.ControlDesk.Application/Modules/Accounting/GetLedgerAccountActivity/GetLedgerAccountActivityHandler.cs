using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountActivity;

public sealed class GetLedgerAccountActivityHandler
{
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly IJournalEntryRepository _journalEntries;

    public GetLedgerAccountActivityHandler(
        ILedgerAccountRepository ledgerAccounts,
        IJournalEntryRepository journalEntries)
    {
        _ledgerAccounts = ledgerAccounts;
        _journalEntries = journalEntries;
    }

    public async Task<Result<GetLedgerAccountActivityResult>> HandleAsync(
        GetLedgerAccountActivityQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.LedgerAccountId == Guid.Empty)
        {
            return Result<GetLedgerAccountActivityResult>.Failure(ApplicationError.Validation(
                nameof(query.LedgerAccountId),
                "Ledger account id is required."));
        }

        if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate.Value > query.ToDate.Value)
        {
            return Result<GetLedgerAccountActivityResult>.Failure(ApplicationError.Validation(
                nameof(query.FromDate),
                "From date cannot be after to date."));
        }

        var ledgerAccountId = LedgerAccountId.Create(query.LedgerAccountId);
        var ledgerAccount = await _ledgerAccounts.GetByIdAsync(ledgerAccountId, cancellationToken);

        if (ledgerAccount is null)
        {
            return Result<GetLedgerAccountActivityResult>.Failure(ApplicationError.NotFound(
                nameof(query.LedgerAccountId),
                "Ledger account was not found."));
        }

        var entries = await _journalEntries.ListForLedgerAccountAsync(
            ledgerAccountId,
            query.FromDate,
            query.ToDate,
            cancellationToken);

        var lines = new List<LedgerAccountActivityLineResult>();
        var runningBalance = 0m;
        string? currencyCode = null;

        foreach (var entry in entries)
        {
            foreach (var journalLine in entry.Lines.Where(line => line.LedgerAccountId == ledgerAccountId))
            {
                currencyCode ??= journalLine.Debit.CurrencyCode;
                runningBalance += GetBalanceMovement(ledgerAccount.NormalBalance, journalLine.Debit.Amount, journalLine.Credit.Amount);

                lines.Add(new LedgerAccountActivityLineResult(
                    entry.Id.Value,
                    entry.EntryDate,
                    entry.SourceType.ToString(),
                    entry.SourceReference,
                    entry.Memo,
                    entry.Status.ToString(),
                    journalLine.Debit.Amount,
                    journalLine.Credit.Amount,
                    runningBalance,
                    journalLine.Debit.CurrencyCode,
                    journalLine.Description));
            }
        }

        return Result<GetLedgerAccountActivityResult>.Success(new GetLedgerAccountActivityResult(
            ledgerAccount.Id.Value,
            ledgerAccount.Code.Value,
            ledgerAccount.Name,
            ledgerAccount.Type.ToString(),
            ledgerAccount.NormalBalance.ToString(),
            query.FromDate,
            query.ToDate,
            currencyCode,
            runningBalance,
            lines));
    }

    private static decimal GetBalanceMovement(NormalBalance normalBalance, decimal debit, decimal credit)
    {
        return normalBalance == NormalBalance.Debit
            ? debit - credit
            : credit - debit;
    }
}
