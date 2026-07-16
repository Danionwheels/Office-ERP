using System.Globalization;
using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetRevenueSummary;

public sealed class GetRevenueSummaryHandler
{
    private const int MaximumReportingWindowYears = 10;

    private readonly IRevenueSummaryReader _revenueSummary;
    private readonly IClock _clock;

    public GetRevenueSummaryHandler(
        IRevenueSummaryReader revenueSummary,
        IClock clock)
    {
        _revenueSummary = revenueSummary;
        _clock = clock;
    }

    public async Task<Result<GetRevenueSummaryResult>> HandleAsync(
        GetRevenueSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        var today = _clock.Today;
        var toDate = query.ToDate ?? today;
        var fromDate = query.FromDate ?? new DateOnly(toDate.Year, toDate.Month, 1);
        var currencyCode = string.IsNullOrWhiteSpace(query.CurrencyCode)
            ? "PKR"
            : query.CurrencyCode.Trim().ToUpperInvariant();

        if (fromDate > toDate)
        {
            return Result<GetRevenueSummaryResult>.Failure(ApplicationError.Validation(
                nameof(query.FromDate),
                "From date cannot be after to date."));
        }

        if (toDate > today)
        {
            return Result<GetRevenueSummaryResult>.Failure(ApplicationError.Validation(
                nameof(query.ToDate),
                "Revenue summary to date cannot be after today."));
        }

        if (ExceedsMaximumWindow(fromDate, toDate))
        {
            return Result<GetRevenueSummaryResult>.Failure(ApplicationError.Validation(
                nameof(query.FromDate),
                $"Revenue summary reporting window cannot exceed {MaximumReportingWindowYears} years."));
        }

        if (!CurrencyCodeValidation.IsValid(currencyCode))
        {
            return Result<GetRevenueSummaryResult>.Failure(ApplicationError.Validation(
                nameof(query.CurrencyCode),
                "Revenue summary currency code must be three ASCII letters."));
        }

        if (!TryParsePeriod(query.Period, out var period))
        {
            return Result<GetRevenueSummaryResult>.Failure(ApplicationError.Validation(
                nameof(query.Period),
                "Revenue summary period must be monthly or quarterly."));
        }

        var buckets = BuildBuckets(fromDate, toDate, period);
        var aggregates = await _revenueSummary.ReadAsync(
            new RevenueSummaryReadRequest(
                fromDate,
                toDate,
                period.ToString(),
                currencyCode),
            cancellationToken);

        foreach (var aggregate in aggregates)
        {
            if (!buckets.TryGetValue(aggregate.PeriodStart, out var bucket))
            {
                continue;
            }

            bucket.Apply(
                aggregate.Debit,
                aggregate.Credit,
                aggregate.ActivityCount);
        }

        var periods = buckets.Values
            .OrderBy(bucket => bucket.PeriodStart)
            .Select(bucket => bucket.ToResult())
            .ToArray();

        return Result<GetRevenueSummaryResult>.Success(new GetRevenueSummaryResult(
            fromDate,
            toDate,
            period.ToString(),
            currencyCode,
            decimal.Round(periods.Sum(item => item.Revenue), 2),
            periods));
    }

    private static SortedDictionary<DateOnly, RevenueBucket> BuildBuckets(
        DateOnly fromDate,
        DateOnly toDate,
        RevenuePeriod period)
    {
        var buckets = new SortedDictionary<DateOnly, RevenueBucket>();
        var cursor = GetPeriodStart(fromDate, period);

        while (true)
        {
            var naturalEnd = GetNaturalPeriodEnd(cursor, period);
            buckets[cursor] = new RevenueBucket(
                cursor < fromDate ? fromDate : cursor,
                naturalEnd > toDate ? toDate : naturalEnd,
                GetLabel(cursor, period));

            if (naturalEnd >= toDate)
            {
                break;
            }

            cursor = naturalEnd.AddDays(1);
        }

        return buckets;
    }

    private static DateOnly GetNaturalPeriodEnd(DateOnly periodStart, RevenuePeriod period)
    {
        var endMonth = period == RevenuePeriod.Monthly
            ? periodStart.Month
            : periodStart.Month + 2;

        return new DateOnly(
            periodStart.Year,
            endMonth,
            DateTime.DaysInMonth(periodStart.Year, endMonth));
    }

    private static bool ExceedsMaximumWindow(DateOnly fromDate, DateOnly toDate)
    {
        if (fromDate.Year > DateOnly.MaxValue.Year - MaximumReportingWindowYears)
        {
            return false;
        }

        var maximumYear = fromDate.Year + MaximumReportingWindowYears;
        var maximumDay = Math.Min(
            fromDate.Day,
            DateTime.DaysInMonth(maximumYear, fromDate.Month));
        var maximumToDate = new DateOnly(maximumYear, fromDate.Month, maximumDay);

        return toDate > maximumToDate;
    }

    private static DateOnly GetPeriodStart(DateOnly date, RevenuePeriod period)
    {
        if (period == RevenuePeriod.Monthly)
        {
            return new DateOnly(date.Year, date.Month, 1);
        }

        var quarterStartMonth = ((date.Month - 1) / 3 * 3) + 1;
        return new DateOnly(date.Year, quarterStartMonth, 1);
    }

    private static string GetLabel(DateOnly periodStart, RevenuePeriod period)
    {
        return period == RevenuePeriod.Monthly
            ? periodStart.ToString("MMM yyyy", CultureInfo.InvariantCulture)
            : $"Q{((periodStart.Month - 1) / 3) + 1} {periodStart.Year}";
    }

    private static bool TryParsePeriod(string? value, out RevenuePeriod period)
    {
        period = RevenuePeriod.Monthly;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "monthly" => true,
            "quarterly" => SetPeriod(RevenuePeriod.Quarterly, out period),
            _ => false
        };
    }

    private static bool SetPeriod(RevenuePeriod value, out RevenuePeriod period)
    {
        period = value;
        return true;
    }

    private enum RevenuePeriod
    {
        Monthly,
        Quarterly
    }

    private sealed class RevenueBucket
    {
        private decimal _debit;
        private decimal _credit;

        public RevenueBucket(DateOnly periodStart, DateOnly periodEnd, string label)
        {
            PeriodStart = periodStart;
            PeriodEnd = periodEnd;
            Label = label;
        }

        public DateOnly PeriodStart { get; }

        public DateOnly PeriodEnd { get; }

        public string Label { get; }

        public int ActivityCount { get; private set; }

        public void Apply(decimal debit, decimal credit, int activityCount)
        {
            _debit += debit;
            _credit += credit;
            ActivityCount += activityCount;
        }

        public RevenueSummaryPeriodResult ToResult()
        {
            return new RevenueSummaryPeriodResult(
                PeriodStart,
                PeriodEnd,
                Label,
                decimal.Round(_debit, 2),
                decimal.Round(_credit, 2),
                decimal.Round(_credit - _debit, 2),
                ActivityCount);
        }
    }
}
