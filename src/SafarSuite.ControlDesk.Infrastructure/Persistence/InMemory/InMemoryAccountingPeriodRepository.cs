using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryAccountingPeriodRepository : IAccountingPeriodRepository
{
    private readonly ConcurrentDictionary<Guid, AccountingPeriod> _periodsById = new();

    public Task AddAsync(AccountingPeriod period, CancellationToken cancellationToken = default)
    {
        _periodsById.TryAdd(period.Id.Value, period);

        return Task.CompletedTask;
    }

    public Task<AccountingPeriod?> GetByIdAsync(
        AccountingPeriodId id,
        CancellationToken cancellationToken = default)
    {
        _periodsById.TryGetValue(id.Value, out var period);

        return Task.FromResult(period);
    }

    public Task<AccountingPeriod?> GetByCompanyAndStartDateAsync(
        string companyCode,
        DateOnly startsOn,
        CancellationToken cancellationToken = default)
    {
        var normalizedCompanyCode = NormalizeCompanyCode(companyCode);
        var period = _periodsById.Values.SingleOrDefault(item =>
            item.StartsOn == startsOn
            && string.Equals(item.CompanyCode, normalizedCompanyCode, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(period);
    }

    public Task<AccountingPeriod?> GetContainingDateAsync(
        string companyCode,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var normalizedCompanyCode = NormalizeCompanyCode(companyCode);
        var period = _periodsById.Values.SingleOrDefault(item =>
            item.Contains(date)
            && string.Equals(item.CompanyCode, normalizedCompanyCode, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(period);
    }

    public Task<IReadOnlyCollection<AccountingPeriod>> ListByCompanyAsync(
        string companyCode,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedCompanyCode = NormalizeCompanyCode(companyCode);
        var periods = _periodsById.Values
            .Where(period => string.Equals(period.CompanyCode, normalizedCompanyCode, StringComparison.OrdinalIgnoreCase));

        if (fromDate.HasValue)
        {
            periods = periods.Where(period => period.EndsOn >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            periods = periods.Where(period => period.StartsOn <= toDate.Value);
        }

        var sorted = periods
            .OrderByDescending(period => period.StartsOn)
            .ThenBy(period => period.Id.Value)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<AccountingPeriod>>(sorted);
    }

    private static string NormalizeCompanyCode(string companyCode)
    {
        return companyCode.Trim().ToUpperInvariant();
    }
}
