using System.Globalization;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListAccountingPeriods;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountingPeriodCloseJournalPreview;

public sealed class GetAccountingPeriodCloseJournalPreviewHandler
{
    private readonly IAccountingPeriodRepository _periods;
    private readonly IAccountingControlSettingsRepository _settings;
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly IJournalEntryRepository _journalEntries;

    public GetAccountingPeriodCloseJournalPreviewHandler(
        IAccountingPeriodRepository periods,
        IAccountingControlSettingsRepository settings,
        ILedgerAccountRepository ledgerAccounts,
        IJournalEntryRepository journalEntries)
    {
        _periods = periods;
        _settings = settings;
        _ledgerAccounts = ledgerAccounts;
        _journalEntries = journalEntries;
    }

    public async Task<Result<GetAccountingPeriodCloseJournalPreviewResult>> HandleAsync(
        GetAccountingPeriodCloseJournalPreviewQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.AccountingPeriodId == Guid.Empty)
        {
            return Result<GetAccountingPeriodCloseJournalPreviewResult>.Failure(ApplicationError.Validation(
                nameof(query.AccountingPeriodId),
                "Accounting period id cannot be empty."));
        }

        var period = await _periods.GetByIdAsync(
            AccountingPeriodId.Create(query.AccountingPeriodId),
            cancellationToken);

        if (period is null)
        {
            return Result<GetAccountingPeriodCloseJournalPreviewResult>.Failure(ApplicationError.NotFound(
                nameof(query.AccountingPeriodId),
                "Accounting period was not found."));
        }

        var companyError = AccountingSetupDefaults.ValidateSingleCompanyCode(
            period.CompanyCode,
            nameof(AccountingPeriod.CompanyCode));

        if (companyError is not null)
        {
            return Result<GetAccountingPeriodCloseJournalPreviewResult>.Failure(companyError);
        }

        var blockers = new List<string>();
        var settings = await _settings.GetByCompanyAsync(period.CompanyCode, cancellationToken);
        var baseCurrencyCode = settings?.BaseCurrencyCode ?? string.Empty;
        var accounts = await _ledgerAccounts.ListAsync(cancellationToken: cancellationToken);
        var accountsById = accounts.ToDictionary(account => account.Id);
        var periodEntries = await _journalEntries.ListAsync(
            period.StartsOn,
            period.EndsOn,
            cancellationToken: cancellationToken);
        var postedEntries = periodEntries
            .Where(entry => entry.Status != JournalEntryStatus.Draft)
            .ToArray();
        var missingLedgerAccountReferences = false;
        var pnlCurrencies = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in postedEntries)
        {
            foreach (var line in entry.Lines)
            {
                if (!accountsById.TryGetValue(line.LedgerAccountId, out var account))
                {
                    missingLedgerAccountReferences = true;
                    continue;
                }

                if (!IsProfitAndLossAccount(account))
                {
                    continue;
                }

                pnlCurrencies.Add(entry.CurrencyCode);
            }
        }

        if (missingLedgerAccountReferences)
        {
            blockers.Add("Posted journal activity references a ledger account that was not found.");
        }

        if (pnlCurrencies.Count == 0)
        {
            return Result<GetAccountingPeriodCloseJournalPreviewResult>.Success(EmptyResult(
                period,
                baseCurrencyCode,
                blockers,
                canGenerate: blockers.Count == 0));
        }

        if (settings is null)
        {
            blockers.Add($"Accounting control settings are not configured for {period.CompanyCode}.");
        }
        else
        {
            if (!settings.IsConfigured)
            {
                blockers.Add("GL controls must include retained earnings, income summary, and rounding accounts.");
            }
        }

        var retainedEarningsAccount = ResolveControlAccount(
            settings?.RetainedEarningsAccountId,
            accountsById,
            "Retained earnings account",
            LedgerAccountType.Equity,
            blockers);
        var incomeSummaryAccount = ResolveControlAccount(
            settings?.IncomeSummaryAccountId,
            accountsById,
            "Income summary account",
            LedgerAccountType.Equity,
            blockers);

        if (settings?.RoundingAccountId is null)
        {
            blockers.Add("Rounding adjustment account is not configured.");
        }
        else if (!accountsById.TryGetValue(settings.RoundingAccountId.Value, out var roundingAccount))
        {
            blockers.Add("Rounding adjustment account was not found.");
        }
        else
        {
            ValidatePostingControlAccount(roundingAccount, "Rounding adjustment account", blockers);
        }

        if (string.IsNullOrWhiteSpace(baseCurrencyCode))
        {
            blockers.Add("Base currency code is not configured.");
        }

        var unsupportedCurrencies = string.IsNullOrWhiteSpace(baseCurrencyCode)
            ? Array.Empty<string>()
            : pnlCurrencies
                .Where(currency => !string.Equals(currency, baseCurrencyCode, StringComparison.Ordinal))
                .OrderBy(currency => currency, StringComparer.Ordinal)
                .ToArray();

        if (unsupportedCurrencies.Length > 0)
        {
            blockers.Add(
                $"Close journal preview only supports base currency {baseCurrencyCode}; found {unsupportedCurrencies[0]} P&L activity.");
        }

        var balances = new Dictionary<LedgerAccountId, ProfitAndLossBalance>();

        if (!string.IsNullOrWhiteSpace(baseCurrencyCode))
        {
            foreach (var entry in postedEntries.Where(entry =>
                string.Equals(entry.CurrencyCode, baseCurrencyCode, StringComparison.Ordinal)))
            {
                foreach (var line in entry.Lines)
                {
                    if (!accountsById.TryGetValue(line.LedgerAccountId, out var account)
                        || !IsProfitAndLossAccount(account))
                    {
                        continue;
                    }

                    if (!balances.TryGetValue(account.Id, out var balance))
                    {
                        balance = new ProfitAndLossBalance(account);
                        balances.Add(account.Id, balance);
                    }

                    balance.Apply(line.Debit.Amount, line.Credit.Amount);
                }
            }
        }

        var closeableBalances = balances.Values
            .Where(balance => balance.NetDebit != 0)
            .OrderBy(balance => balance.Account.Code.Value, StringComparer.Ordinal)
            .ToArray();

        if (closeableBalances.Length == 0 && unsupportedCurrencies.Length == 0 && blockers.Count == 0)
        {
            return Result<GetAccountingPeriodCloseJournalPreviewResult>.Success(EmptyResult(
                period,
                baseCurrencyCode,
                Array.Empty<string>(),
                canGenerate: true));
        }

        if (blockers.Count > 0 || retainedEarningsAccount is null || incomeSummaryAccount is null)
        {
            return Result<GetAccountingPeriodCloseJournalPreviewResult>.Success(EmptyResult(
                period,
                baseCurrencyCode,
                blockers,
                canGenerate: false));
        }

        var preview = BuildPreview(
            period,
            baseCurrencyCode,
            retainedEarningsAccount,
            incomeSummaryAccount,
            closeableBalances);

        return Result<GetAccountingPeriodCloseJournalPreviewResult>.Success(preview);
    }

    private static GetAccountingPeriodCloseJournalPreviewResult BuildPreview(
        AccountingPeriod period,
        string baseCurrencyCode,
        LedgerAccount retainedEarningsAccount,
        LedgerAccount incomeSummaryAccount,
        IReadOnlyCollection<ProfitAndLossBalance> balances)
    {
        var periodToken = period.EndsOn.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var closeLines = new List<AccountingCloseJournalPreviewLineResult>();
        var closeDebit = 0m;
        var closeCredit = 0m;

        foreach (var balance in balances)
        {
            var amount = Math.Abs(balance.NetDebit);

            if (balance.NetDebit > 0)
            {
                closeCredit += amount;
                closeLines.Add(ToLineResult(
                    balance.Account,
                    debit: 0m,
                    credit: amount,
                    $"Close {balance.Account.Code.Value} to income summary."));
            }
            else
            {
                closeDebit += amount;
                closeLines.Add(ToLineResult(
                    balance.Account,
                    debit: amount,
                    credit: 0m,
                    $"Close {balance.Account.Code.Value} to income summary."));
            }
        }

        var netIncome = decimal.Round(closeDebit - closeCredit, 2);
        var incomeSummaryAmount = Math.Abs(netIncome);
        var entries = new List<AccountingCloseJournalPreviewEntryResult>();

        if (incomeSummaryAmount > 0)
        {
            if (netIncome > 0)
            {
                closeCredit += incomeSummaryAmount;
                closeLines.Add(ToLineResult(
                    incomeSummaryAccount,
                    debit: 0m,
                    credit: incomeSummaryAmount,
                    $"Record net income for {period.Name}."));
            }
            else
            {
                closeDebit += incomeSummaryAmount;
                closeLines.Add(ToLineResult(
                    incomeSummaryAccount,
                    debit: incomeSummaryAmount,
                    credit: 0m,
                    $"Record net loss for {period.Name}."));
            }
        }

        entries.Add(new AccountingCloseJournalPreviewEntryResult(
            $"CLOSE-{periodToken}",
            $"Close revenue and expense for {period.Name}",
            period.EndsOn,
            baseCurrencyCode,
            decimal.Round(closeDebit, 2),
            decimal.Round(closeCredit, 2),
            closeLines));

        if (incomeSummaryAmount > 0)
        {
            var retainedLines = netIncome > 0
                ? new[]
                {
                    ToLineResult(
                        incomeSummaryAccount,
                        debit: incomeSummaryAmount,
                        credit: 0m,
                        $"Close income summary for {period.Name}."),
                    ToLineResult(
                        retainedEarningsAccount,
                        debit: 0m,
                        credit: incomeSummaryAmount,
                        $"Transfer net income to retained earnings for {period.Name}.")
                }
                : new[]
                {
                    ToLineResult(
                        retainedEarningsAccount,
                        debit: incomeSummaryAmount,
                        credit: 0m,
                        $"Transfer net loss to retained earnings for {period.Name}."),
                    ToLineResult(
                        incomeSummaryAccount,
                        debit: 0m,
                        credit: incomeSummaryAmount,
                        $"Close income summary for {period.Name}.")
                };

            entries.Add(new AccountingCloseJournalPreviewEntryResult(
                $"RE-{periodToken}",
                $"Close income summary to retained earnings for {period.Name}",
                period.EndsOn,
                baseCurrencyCode,
                incomeSummaryAmount,
                incomeSummaryAmount,
                retainedLines));
        }

        return new GetAccountingPeriodCloseJournalPreviewResult(
            ListAccountingPeriodsHandler.ToResult(period),
            baseCurrencyCode,
            true,
            netIncome,
            entries.Sum(entry => entry.TotalDebit),
            entries.Sum(entry => entry.TotalCredit),
            Array.Empty<string>(),
            entries);
    }

    private static GetAccountingPeriodCloseJournalPreviewResult EmptyResult(
        AccountingPeriod period,
        string baseCurrencyCode,
        IReadOnlyCollection<string> blockers,
        bool canGenerate)
    {
        return new GetAccountingPeriodCloseJournalPreviewResult(
            ListAccountingPeriodsHandler.ToResult(period),
            baseCurrencyCode,
            canGenerate,
            0m,
            0m,
            0m,
            blockers,
            Array.Empty<AccountingCloseJournalPreviewEntryResult>());
    }

    private static LedgerAccount? ResolveControlAccount(
        LedgerAccountId? accountId,
        IReadOnlyDictionary<LedgerAccountId, LedgerAccount> accountsById,
        string label,
        LedgerAccountType expectedType,
        ICollection<string> blockers)
    {
        if (accountId is null)
        {
            blockers.Add($"{label} is not configured.");
            return null;
        }

        if (!accountsById.TryGetValue(accountId.Value, out var account))
        {
            blockers.Add($"{label} was not found.");
            return null;
        }

        ValidatePostingControlAccount(account, label, blockers);

        if (account.Type != expectedType)
        {
            blockers.Add($"{label} must be a {expectedType} account.");
            return null;
        }

        return account;
    }

    private static void ValidatePostingControlAccount(
        LedgerAccount account,
        string label,
        ICollection<string> blockers)
    {
        if (!account.IsPostingAccount)
        {
            blockers.Add($"{label} must be a posting account.");
        }

        if (account.Status != LedgerAccountStatus.Active)
        {
            blockers.Add($"{label} must be active.");
        }
    }

    private static AccountingCloseJournalPreviewLineResult ToLineResult(
        LedgerAccount account,
        decimal debit,
        decimal credit,
        string description)
    {
        return new AccountingCloseJournalPreviewLineResult(
            account.Id.Value,
            account.Code.Value,
            account.Name,
            account.Type.ToString(),
            decimal.Round(debit, 2),
            decimal.Round(credit, 2),
            description);
    }

    private static bool IsProfitAndLossAccount(LedgerAccount account)
    {
        return account.Type is LedgerAccountType.Revenue or LedgerAccountType.Expense;
    }

    private sealed class ProfitAndLossBalance
    {
        private decimal _debit;
        private decimal _credit;

        public ProfitAndLossBalance(LedgerAccount account)
        {
            Account = account;
        }

        public LedgerAccount Account { get; }

        public decimal NetDebit => decimal.Round(_debit - _credit, 2);

        public void Apply(decimal debit, decimal credit)
        {
            _debit += debit;
            _credit += credit;
        }
    }
}
