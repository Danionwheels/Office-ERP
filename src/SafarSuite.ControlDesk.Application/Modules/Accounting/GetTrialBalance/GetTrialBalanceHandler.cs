using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetTrialBalance;

public sealed class GetTrialBalanceHandler
{
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly IJournalEntryRepository _journalEntries;
    private readonly IClock _clock;

    public GetTrialBalanceHandler(
        ILedgerAccountRepository ledgerAccounts,
        IJournalEntryRepository journalEntries,
        IClock clock)
    {
        _ledgerAccounts = ledgerAccounts;
        _journalEntries = journalEntries;
        _clock = clock;
    }

    public async Task<Result<GetTrialBalanceResult>> HandleAsync(
        GetTrialBalanceQuery query,
        CancellationToken cancellationToken = default)
    {
        var asOfDate = query.AsOfDate ?? _clock.Today;
        var currencyCode = string.IsNullOrWhiteSpace(query.CurrencyCode)
            ? "PKR"
            : query.CurrencyCode.Trim().ToUpperInvariant();

        if (currencyCode.Length != 3)
        {
            return Result<GetTrialBalanceResult>.Failure(ApplicationError.Validation(
                nameof(query.CurrencyCode),
                "Trial balance currency code must be three characters."));
        }

        var accounts = await _ledgerAccounts.ListAsync(cancellationToken: cancellationToken);
        var accountBalances = accounts.ToDictionary(
            account => account.Id,
            account => new AccountBalance(account));
        var entries = await _journalEntries.ListAsync(
            toDate: asOfDate,
            cancellationToken: cancellationToken);

        foreach (var entry in entries.Where(entry =>
            entry.Status != JournalEntryStatus.Draft
            && string.Equals(entry.CurrencyCode, currencyCode, StringComparison.Ordinal)))
        {
            foreach (var line in entry.Lines)
            {
                if (!accountBalances.TryGetValue(line.LedgerAccountId, out var balance))
                {
                    continue;
                }

                balance.Apply(line.Debit.Amount, line.Credit.Amount);
            }
        }

        var lines = accountBalances.Values
            .Where(balance => balance.ActivityCount > 0 || balance.Account.Status == LedgerAccountStatus.Active)
            .OrderBy(balance => balance.Account.Code.Value, StringComparer.Ordinal)
            .ThenBy(balance => balance.Account.Name, StringComparer.OrdinalIgnoreCase)
            .Select(balance => balance.ToResult())
            .ToArray();
        var totalDebit = lines.Sum(line => line.DebitBalance);
        var totalCredit = lines.Sum(line => line.CreditBalance);

        return Result<GetTrialBalanceResult>.Success(new GetTrialBalanceResult(
            asOfDate,
            currencyCode,
            totalDebit,
            totalCredit,
            totalDebit - totalCredit,
            lines));
    }

    private sealed class AccountBalance
    {
        private decimal _debit;
        private decimal _credit;

        public AccountBalance(LedgerAccount account)
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

        public TrialBalanceLineResult ToResult()
        {
            var netBalance = Account.NormalBalance == NormalBalance.Debit
                ? _debit - _credit
                : _credit - _debit;
            var debitBalance = 0m;
            var creditBalance = 0m;

            if (netBalance > 0)
            {
                if (Account.NormalBalance == NormalBalance.Debit)
                {
                    debitBalance = netBalance;
                }
                else
                {
                    creditBalance = netBalance;
                }
            }
            else if (netBalance < 0)
            {
                if (Account.NormalBalance == NormalBalance.Debit)
                {
                    creditBalance = Math.Abs(netBalance);
                }
                else
                {
                    debitBalance = Math.Abs(netBalance);
                }
            }

            return new TrialBalanceLineResult(
                Account.Id.Value,
                Account.Code.Value,
                Account.Name,
                Account.Type.ToString(),
                Account.NormalBalance.ToString(),
                decimal.Round(debitBalance, 2),
                decimal.Round(creditBalance, 2),
                decimal.Round(netBalance, 2),
                ActivityCount);
        }
    }
}
