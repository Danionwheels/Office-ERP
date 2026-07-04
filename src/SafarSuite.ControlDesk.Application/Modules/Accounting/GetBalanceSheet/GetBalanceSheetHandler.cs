using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetBalanceSheet;

public sealed class GetBalanceSheetHandler
{
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly IJournalEntryRepository _journalEntries;
    private readonly IClock _clock;

    public GetBalanceSheetHandler(
        ILedgerAccountRepository ledgerAccounts,
        IJournalEntryRepository journalEntries,
        IClock clock)
    {
        _ledgerAccounts = ledgerAccounts;
        _journalEntries = journalEntries;
        _clock = clock;
    }

    public async Task<Result<GetBalanceSheetResult>> HandleAsync(
        GetBalanceSheetQuery query,
        CancellationToken cancellationToken = default)
    {
        var asOfDate = query.AsOfDate ?? _clock.Today;
        var currencyCode = string.IsNullOrWhiteSpace(query.CurrencyCode)
            ? "PKR"
            : query.CurrencyCode.Trim().ToUpperInvariant();

        if (currencyCode.Length != 3)
        {
            return Result<GetBalanceSheetResult>.Failure(ApplicationError.Validation(
                nameof(query.CurrencyCode),
                "Balance sheet currency code must be three characters."));
        }

        var accounts = await _ledgerAccounts.ListAsync(cancellationToken: cancellationToken);
        var accountsById = accounts.ToDictionary(account => account.Id);
        var balanceSheetAccountsById = accounts
            .Where(IsBalanceSheetAccount)
            .ToDictionary(
                account => account.Id,
                account => new BalanceSheetAccountBalance(account));
        var currentEarnings = new CurrentEarningsBalance();
        var entries = await _journalEntries.ListAsync(
            toDate: asOfDate,
            cancellationToken: cancellationToken);

        foreach (var entry in entries.Where(entry =>
            entry.Status != JournalEntryStatus.Draft
            && string.Equals(entry.CurrencyCode, currencyCode, StringComparison.Ordinal)))
        {
            foreach (var line in entry.Lines)
            {
                if (!accountsById.TryGetValue(line.LedgerAccountId, out var account))
                {
                    continue;
                }

                if (balanceSheetAccountsById.TryGetValue(line.LedgerAccountId, out var balance))
                {
                    balance.Apply(line.Debit.Amount, line.Credit.Amount);
                }
                else if (IsProfitAndLossAccount(account))
                {
                    currentEarnings.Apply(account.Type, line.Debit.Amount, line.Credit.Amount);
                }
            }
        }

        var assetLines = BuildSectionLines(
            balanceSheetAccountsById.Values,
            LedgerAccountType.Asset);
        var liabilityLines = BuildSectionLines(
            balanceSheetAccountsById.Values,
            LedgerAccountType.Liability);
        var equityLines = BuildSectionLines(
            balanceSheetAccountsById.Values,
            LedgerAccountType.Equity)
            .ToList();

        if (currentEarnings.HasBalance)
        {
            equityLines.Add(currentEarnings.ToResult());
        }

        var totalAssets = decimal.Round(assetLines.Sum(line => line.Amount), 2);
        var totalLiabilities = decimal.Round(liabilityLines.Sum(line => line.Amount), 2);
        var totalEquity = decimal.Round(equityLines.Sum(line => line.Amount), 2);
        var totalLiabilitiesAndEquity = decimal.Round(totalLiabilities + totalEquity, 2);
        var difference = decimal.Round(totalAssets - totalLiabilitiesAndEquity, 2);

        return Result<GetBalanceSheetResult>.Success(new GetBalanceSheetResult(
            asOfDate,
            currencyCode,
            totalAssets,
            totalLiabilities,
            totalEquity,
            totalLiabilitiesAndEquity,
            difference,
            [
                new BalanceSheetSectionResult(
                    LedgerAccountType.Asset.ToString(),
                    "Assets",
                    totalAssets,
                    assetLines),
                new BalanceSheetSectionResult(
                    LedgerAccountType.Liability.ToString(),
                    "Liabilities",
                    totalLiabilities,
                    liabilityLines),
                new BalanceSheetSectionResult(
                    LedgerAccountType.Equity.ToString(),
                    "Equity",
                    totalEquity,
                    equityLines)
            ]));
    }

    private static IReadOnlyCollection<BalanceSheetLineResult> BuildSectionLines(
        IEnumerable<BalanceSheetAccountBalance> balances,
        LedgerAccountType accountType)
    {
        return balances
            .Where(balance =>
                balance.Account.Type == accountType
                && (balance.Amount != 0m || balance.ActivityCount > 0))
            .OrderBy(balance => balance.Account.Code.Value, StringComparer.Ordinal)
            .ThenBy(balance => balance.Account.Name, StringComparer.OrdinalIgnoreCase)
            .Select(balance => balance.ToResult())
            .ToArray();
    }

    private static bool IsBalanceSheetAccount(LedgerAccount account)
    {
        return account.Type is
            LedgerAccountType.Asset or
            LedgerAccountType.Liability or
            LedgerAccountType.Equity;
    }

    private static bool IsProfitAndLossAccount(LedgerAccount account)
    {
        return account.Type is LedgerAccountType.Revenue or LedgerAccountType.Expense;
    }

    private sealed class BalanceSheetAccountBalance
    {
        private decimal _debit;
        private decimal _credit;

        public BalanceSheetAccountBalance(LedgerAccount account)
        {
            Account = account;
        }

        public LedgerAccount Account { get; }

        public int ActivityCount { get; private set; }

        public decimal Amount =>
            Account.Type == LedgerAccountType.Asset
                ? _debit - _credit
                : _credit - _debit;

        public void Apply(decimal debit, decimal credit)
        {
            _debit += debit;
            _credit += credit;
            ActivityCount++;
        }

        public BalanceSheetLineResult ToResult()
        {
            return new BalanceSheetLineResult(
                Account.Id.Value,
                Account.Code.Value,
                Account.Name,
                Account.Type.ToString(),
                Account.NormalBalance.ToString(),
                decimal.Round(_debit, 2),
                decimal.Round(_credit, 2),
                decimal.Round(Amount, 2),
                ActivityCount,
                false);
        }
    }

    private sealed class CurrentEarningsBalance
    {
        private decimal _revenueDebit;
        private decimal _revenueCredit;
        private decimal _expenseDebit;
        private decimal _expenseCredit;

        public int ActivityCount { get; private set; }

        public bool HasBalance => decimal.Round(Amount, 2) != 0m;

        public decimal Amount =>
            (_revenueCredit - _revenueDebit)
            - (_expenseDebit - _expenseCredit);

        public void Apply(LedgerAccountType accountType, decimal debit, decimal credit)
        {
            if (accountType == LedgerAccountType.Revenue)
            {
                _revenueDebit += debit;
                _revenueCredit += credit;
            }
            else if (accountType == LedgerAccountType.Expense)
            {
                _expenseDebit += debit;
                _expenseCredit += credit;
            }

            ActivityCount++;
        }

        public BalanceSheetLineResult ToResult()
        {
            return new BalanceSheetLineResult(
                null,
                "CUR-EARN",
                "Current earnings",
                LedgerAccountType.Equity.ToString(),
                NormalBalance.Credit.ToString(),
                decimal.Round(_expenseDebit + _revenueDebit, 2),
                decimal.Round(_expenseCredit + _revenueCredit, 2),
                decimal.Round(Amount, 2),
                ActivityCount,
                true);
        }
    }
}
