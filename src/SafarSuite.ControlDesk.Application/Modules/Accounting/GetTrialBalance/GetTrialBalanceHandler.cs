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

        if (query.FromDate.HasValue && query.FromDate.Value > asOfDate)
        {
            return Result<GetTrialBalanceResult>.Failure(ApplicationError.Validation(
                nameof(query.FromDate),
                "From date cannot be after as-of date."));
        }

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
            var isOpeningEntry = query.FromDate.HasValue && entry.EntryDate < query.FromDate.Value;

            foreach (var line in entry.Lines)
            {
                if (!accountBalances.TryGetValue(line.LedgerAccountId, out var balance))
                {
                    continue;
                }

                if (isOpeningEntry)
                {
                    balance.ApplyOpening(line.Debit.Amount, line.Credit.Amount);
                }
                else
                {
                    balance.ApplyPeriod(line.Debit.Amount, line.Credit.Amount);
                }
            }
        }

        var lines = accountBalances.Values
            .Where(balance => balance.HasBalance || balance.Account.Status == LedgerAccountStatus.Active)
            .OrderBy(balance => balance.Account.Code.Value, StringComparer.Ordinal)
            .ThenBy(balance => balance.Account.Name, StringComparer.OrdinalIgnoreCase)
            .Select(balance => balance.ToResult())
            .ToArray();
        var totalDebit = lines.Sum(line => line.DebitBalance);
        var totalCredit = lines.Sum(line => line.CreditBalance);
        var totalPeriodDebit = lines.Sum(line => line.PeriodDebit);
        var totalPeriodCredit = lines.Sum(line => line.PeriodCredit);

        return Result<GetTrialBalanceResult>.Success(new GetTrialBalanceResult(
            query.FromDate,
            asOfDate,
            currencyCode,
            totalDebit,
            totalCredit,
            totalPeriodDebit,
            totalPeriodCredit,
            totalDebit - totalCredit,
            lines));
    }

    private sealed class AccountBalance
    {
        private decimal _openingDebit;
        private decimal _openingCredit;
        private decimal _periodDebit;
        private decimal _periodCredit;

        public AccountBalance(LedgerAccount account)
        {
            Account = account;
        }

        public LedgerAccount Account { get; }

        public int ActivityCount { get; private set; }

        public bool HasBalance =>
            _openingDebit != 0m
            || _openingCredit != 0m
            || _periodDebit != 0m
            || _periodCredit != 0m;

        public void ApplyOpening(decimal debit, decimal credit)
        {
            _openingDebit += debit;
            _openingCredit += credit;
        }

        public void ApplyPeriod(decimal debit, decimal credit)
        {
            _periodDebit += debit;
            _periodCredit += credit;
            ActivityCount++;
        }

        public TrialBalanceLineResult ToResult()
        {
            var openingBalance = GetNormalBalance(_openingDebit, _openingCredit);
            var periodMovement = GetNormalBalance(_periodDebit, _periodCredit);
            var closingBalance = openingBalance + periodMovement;
            var (debitBalance, creditBalance) = ToDebitCreditBalance(closingBalance);

            return new TrialBalanceLineResult(
                Account.Id.Value,
                Account.Code.Value,
                Account.Name,
                Account.Type.ToString(),
                Account.NormalBalance.ToString(),
                decimal.Round(openingBalance, 2),
                decimal.Round(_periodDebit, 2),
                decimal.Round(_periodCredit, 2),
                decimal.Round(debitBalance, 2),
                decimal.Round(creditBalance, 2),
                decimal.Round(closingBalance, 2),
                ActivityCount);
        }

        private decimal GetNormalBalance(decimal debit, decimal credit)
        {
            return Account.NormalBalance == NormalBalance.Debit
                ? debit - credit
                : credit - debit;
        }

        private (decimal DebitBalance, decimal CreditBalance) ToDebitCreditBalance(decimal netBalance)
        {
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

            return (debitBalance, creditBalance);
        }
    }
}
