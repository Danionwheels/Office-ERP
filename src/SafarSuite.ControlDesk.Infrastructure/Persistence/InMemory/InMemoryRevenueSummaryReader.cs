using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryRevenueSummaryReader : IRevenueSummaryReader
{
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly IJournalEntryRepository _journalEntries;

    public InMemoryRevenueSummaryReader(
        ILedgerAccountRepository ledgerAccounts,
        IJournalEntryRepository journalEntries)
    {
        _ledgerAccounts = ledgerAccounts;
        _journalEntries = journalEntries;
    }

    public async Task<IReadOnlyCollection<RevenueSummaryPeriodReadModel>> ReadAsync(
        RevenueSummaryReadRequest request,
        CancellationToken cancellationToken = default)
    {
        var revenueAccounts = await _ledgerAccounts.ListAsync(
            type: LedgerAccountType.Revenue,
            cancellationToken: cancellationToken);
        var revenueAccountIds = revenueAccounts
            .Select(account => account.Id)
            .ToHashSet();
        var entries = await _journalEntries.ListAsync(
            request.FromDate,
            request.ToDate,
            cancellationToken: cancellationToken);
        var buckets = new SortedDictionary<DateOnly, RevenueAggregate>();

        foreach (var entry in entries.Where(entry =>
            entry.Status != JournalEntryStatus.Draft
            && entry.SourceType is not JournalSourceType.PeriodClose
            && entry.SourceType is not JournalSourceType.PeriodCloseReversal
            && string.Equals(entry.CurrencyCode, request.CurrencyCode, StringComparison.Ordinal)))
        {
            var revenueLines = entry.Lines
                .Where(line => revenueAccountIds.Contains(line.LedgerAccountId))
                .ToArray();

            if (revenueLines.Length == 0)
            {
                continue;
            }

            var periodStart = GetPeriodStart(entry.EntryDate, request.Period);
            if (!buckets.TryGetValue(periodStart, out var bucket))
            {
                bucket = new RevenueAggregate();
                buckets.Add(periodStart, bucket);
            }

            bucket.Apply(
                revenueLines.Sum(line => line.Debit.Amount),
                revenueLines.Sum(line => line.Credit.Amount));
        }

        return buckets
            .Select(bucket => new RevenueSummaryPeriodReadModel(
                bucket.Key,
                bucket.Value.Debit,
                bucket.Value.Credit,
                bucket.Value.ActivityCount))
            .ToArray();
    }

    private static DateOnly GetPeriodStart(DateOnly date, string period)
    {
        if (!string.Equals(period, "Quarterly", StringComparison.Ordinal))
        {
            return new DateOnly(date.Year, date.Month, 1);
        }

        var quarterStartMonth = ((date.Month - 1) / 3 * 3) + 1;
        return new DateOnly(date.Year, quarterStartMonth, 1);
    }

    private sealed class RevenueAggregate
    {
        public decimal Debit { get; private set; }

        public decimal Credit { get; private set; }

        public int ActivityCount { get; private set; }

        public void Apply(decimal debit, decimal credit)
        {
            Debit += debit;
            Credit += credit;
            ActivityCount++;
        }
    }
}
