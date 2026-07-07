using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetProfitAndLossStatement;

public sealed class GetProfitAndLossStatementHandler
{
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly IJournalEntryRepository _journalEntries;
    private readonly IClock _clock;

    public GetProfitAndLossStatementHandler(
        ILedgerAccountRepository ledgerAccounts,
        IJournalEntryRepository journalEntries,
        IClock clock)
    {
        _ledgerAccounts = ledgerAccounts;
        _journalEntries = journalEntries;
        _clock = clock;
    }

    public async Task<Result<GetProfitAndLossStatementResult>> HandleAsync(
        GetProfitAndLossStatementQuery query,
        CancellationToken cancellationToken = default)
    {
        var toDate = query.ToDate ?? _clock.Today;
        var currencyCode = string.IsNullOrWhiteSpace(query.CurrencyCode)
            ? "PKR"
            : query.CurrencyCode.Trim().ToUpperInvariant();

        if (query.FromDate.HasValue && query.FromDate.Value > toDate)
        {
            return Result<GetProfitAndLossStatementResult>.Failure(ApplicationError.Validation(
                nameof(query.FromDate),
                "From date cannot be after to date."));
        }

        if (currencyCode.Length != 3)
        {
            return Result<GetProfitAndLossStatementResult>.Failure(ApplicationError.Validation(
                nameof(query.CurrencyCode),
                "Profit and loss currency code must be three characters."));
        }

        var accounts = await _ledgerAccounts.ListAsync(cancellationToken: cancellationToken);
        var pnlAccountsById = accounts
            .Where(IsProfitAndLossAccount)
            .ToDictionary(
                account => account.Id,
                account => new ProfitAndLossAccountBalance(account));
        var entries = await _journalEntries.ListAsync(
            query.FromDate,
            toDate,
            cancellationToken: cancellationToken);

        foreach (var entry in entries.Where(entry =>
            entry.Status != JournalEntryStatus.Draft
            && string.Equals(entry.CurrencyCode, currencyCode, StringComparison.Ordinal)))
        {
            foreach (var line in entry.Lines)
            {
                if (!pnlAccountsById.TryGetValue(line.LedgerAccountId, out var balance))
                {
                    continue;
                }

                balance.Apply(line.Debit.Amount, line.Credit.Amount);
            }
        }

        var revenueLines = BuildSectionLines(
            pnlAccountsById.Values,
            LedgerAccountType.Revenue);
        var expenseLines = BuildSectionLines(
            pnlAccountsById.Values,
            LedgerAccountType.Expense);
        var totalRevenue = revenueLines.Sum(line => line.Amount);
        var totalExpense = expenseLines.Sum(line => line.Amount);

        return Result<GetProfitAndLossStatementResult>.Success(new GetProfitAndLossStatementResult(
            query.FromDate,
            toDate,
            currencyCode,
            decimal.Round(totalRevenue, 2),
            decimal.Round(totalExpense, 2),
            decimal.Round(totalRevenue - totalExpense, 2),
            [
                new ProfitAndLossStatementSectionResult(
                    LedgerAccountType.Revenue.ToString(),
                    "Revenue",
                    decimal.Round(totalRevenue, 2),
                    revenueLines),
                new ProfitAndLossStatementSectionResult(
                    LedgerAccountType.Expense.ToString(),
                    "Expense",
                    decimal.Round(totalExpense, 2),
                    expenseLines)
            ]));
    }

    private static IReadOnlyCollection<ProfitAndLossStatementLineResult> BuildSectionLines(
        IEnumerable<ProfitAndLossAccountBalance> balances,
        LedgerAccountType accountType)
    {
        return balances
            .Where(balance =>
                balance.Account.Type == accountType
                && balance.ActivityCount > 0)
            .OrderBy(balance => balance.Account.Code.Value, StringComparer.Ordinal)
            .ThenBy(balance => balance.Account.Name, StringComparer.OrdinalIgnoreCase)
            .Select(balance => balance.ToResult())
            .ToArray();
    }

    private static bool IsProfitAndLossAccount(LedgerAccount account)
    {
        return account.Type is LedgerAccountType.Revenue or LedgerAccountType.Expense;
    }

    private sealed class ProfitAndLossAccountBalance
    {
        private decimal _debit;
        private decimal _credit;

        public ProfitAndLossAccountBalance(LedgerAccount account)
        {
            Account = account;
        }

        public LedgerAccount Account { get; }

        public int ActivityCount { get; private set; }

        public void Apply(decimal debit, decimal credit)
        {
            _debit += debit;
            _credit += credit;
            ActivityCount++;
        }

        public ProfitAndLossStatementLineResult ToResult()
        {
            var amount = Account.Type == LedgerAccountType.Revenue
                ? _credit - _debit
                : _debit - _credit;

            return new ProfitAndLossStatementLineResult(
                Account.Id.Value,
                Account.Code.Value,
                Account.Name,
                Account.Type.ToString(),
                Account.NormalBalance.ToString(),
                decimal.Round(_debit, 2),
                decimal.Round(_credit, 2),
                decimal.Round(amount, 2),
                ActivityCount);
        }
    }
}
