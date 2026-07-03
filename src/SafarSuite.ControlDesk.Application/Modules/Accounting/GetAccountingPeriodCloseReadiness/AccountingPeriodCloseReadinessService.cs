using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListAccountingPeriods;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountingPeriodCloseReadiness;

public sealed class AccountingPeriodCloseReadinessService
{
    private const string Passed = "Passed";
    private const string Blocked = "Blocked";

    private readonly IAccountingPeriodRepository _periods;
    private readonly IJournalEntryRepository _journalEntries;

    public AccountingPeriodCloseReadinessService(
        IAccountingPeriodRepository periods,
        IJournalEntryRepository journalEntries)
    {
        _periods = periods;
        _journalEntries = journalEntries;
    }

    public async Task<Result<GetAccountingPeriodCloseReadinessResult>> CheckAsync(
        Guid accountingPeriodId,
        CancellationToken cancellationToken = default)
    {
        if (accountingPeriodId == Guid.Empty)
        {
            return Result<GetAccountingPeriodCloseReadinessResult>.Failure(ApplicationError.Validation(
                nameof(accountingPeriodId),
                "Accounting period id cannot be empty."));
        }

        var period = await _periods.GetByIdAsync(
            AccountingPeriodId.Create(accountingPeriodId),
            cancellationToken);

        if (period is null)
        {
            return Result<GetAccountingPeriodCloseReadinessResult>.Failure(ApplicationError.NotFound(
                nameof(accountingPeriodId),
                "Accounting period was not found."));
        }

        var companyError = AccountingSetupDefaults.ValidateSingleCompanyCode(
            period.CompanyCode,
            nameof(AccountingPeriod.CompanyCode));

        if (companyError is not null)
        {
            return Result<GetAccountingPeriodCloseReadinessResult>.Failure(companyError);
        }

        var checks = new List<AccountingPeriodCloseReadinessCheckResult>
        {
            period.Status == AccountingPeriodStatus.Open
                ? PassedCheck(
                    "PeriodOpen",
                    $"Accounting period {period.Name} is open.",
                    nameof(period.Status))
                : BlockedCheck(
                    "PeriodOpen",
                    $"Accounting period {period.Name} is already closed.",
                    nameof(period.Status))
        };

        var priorOpenPeriods = (await _periods.ListByCompanyAsync(
                period.CompanyCode,
                toDate: period.StartsOn.AddDays(-1),
                cancellationToken: cancellationToken))
            .Where(candidate => candidate.Status == AccountingPeriodStatus.Open)
            .OrderBy(candidate => candidate.StartsOn)
            .ToArray();

        checks.Add(priorOpenPeriods.Length == 0
            ? PassedCheck(
                "PriorPeriodsClosed",
                "Earlier accounting periods are closed.",
                nameof(period.StartsOn))
            : BlockedCheck(
                "PriorPeriodsClosed",
                $"Close earlier open period {priorOpenPeriods[0].Name} first.",
                nameof(period.StartsOn)));

        var periodEntries = await _journalEntries.ListAsync(
            period.StartsOn,
            period.EndsOn,
            cancellationToken: cancellationToken);

        var draftCount = periodEntries.Count(entry => entry.Status == JournalEntryStatus.Draft);

        checks.Add(draftCount == 0
            ? PassedCheck(
                "NoDraftJournals",
                "No draft journal entries are dated inside this period.",
                "JournalEntries")
            : BlockedCheck(
                "NoDraftJournals",
                $"{draftCount} draft journal entries are dated inside this period.",
                "JournalEntries"));

        var currencies = periodEntries
            .GroupBy(entry => entry.CurrencyCode, StringComparer.Ordinal)
            .Select(group => ToCurrencyResult(group.Key, group))
            .OrderBy(currency => currency.CurrencyCode, StringComparer.Ordinal)
            .ToArray();
        var unbalancedCurrencies = currencies
            .Where(currency => currency.Difference != 0)
            .ToArray();

        checks.Add(unbalancedCurrencies.Length == 0
            ? PassedCheck(
                "BalancedCurrencyActivity",
                currencies.Length == 0
                    ? "No journal activity is dated inside this period."
                    : "Journal activity balances by currency.",
                "JournalEntries")
            : BlockedCheck(
                "BalancedCurrencyActivity",
                $"Journal activity is out of balance for {unbalancedCurrencies[0].CurrencyCode}.",
                "JournalEntries"));

        var canClose = checks.All(check => check.Status != Blocked);

        return Result<GetAccountingPeriodCloseReadinessResult>.Success(new GetAccountingPeriodCloseReadinessResult(
            ListAccountingPeriodsHandler.ToResult(period),
            canClose,
            checks,
            currencies));
    }

    private static AccountingPeriodCloseCurrencyResult ToCurrencyResult(
        string currencyCode,
        IEnumerable<JournalEntry> entries)
    {
        var entryList = entries.ToArray();
        var postedEntries = entryList
            .Where(entry => entry.Status != JournalEntryStatus.Draft)
            .ToArray();
        var totalDebit = postedEntries.Sum(entry => entry.TotalDebit.Amount);
        var totalCredit = postedEntries.Sum(entry => entry.TotalCredit.Amount);

        return new AccountingPeriodCloseCurrencyResult(
            currencyCode,
            decimal.Round(totalDebit, 2),
            decimal.Round(totalCredit, 2),
            decimal.Round(totalDebit - totalCredit, 2),
            postedEntries.Length,
            entryList.Length - postedEntries.Length);
    }

    private static AccountingPeriodCloseReadinessCheckResult PassedCheck(
        string code,
        string message,
        string? target)
    {
        return new AccountingPeriodCloseReadinessCheckResult(code, Passed, message, target);
    }

    private static AccountingPeriodCloseReadinessCheckResult BlockedCheck(
        string code,
        string message,
        string? target)
    {
        return new AccountingPeriodCloseReadinessCheckResult(code, Blocked, message, target);
    }
}
