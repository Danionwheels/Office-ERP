using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryAccountCodeRangeRepository : IAccountCodeRangeRepository
{
    private readonly ConcurrentDictionary<Guid, AccountCodeRange> _rangesById = new();

    public Task AddAsync(AccountCodeRange range, CancellationToken cancellationToken = default)
    {
        _rangesById.TryAdd(range.Id.Value, range);

        return Task.CompletedTask;
    }

    public Task<bool> AnyByCompanyAsync(string companyCode, CancellationToken cancellationToken = default)
    {
        var normalizedCompanyCode = NormalizeCompanyCode(companyCode);
        var exists = _rangesById.Values.Any(range =>
            range.CompanyCode.Equals(normalizedCompanyCode, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(exists);
    }

    public Task<AccountCodeRange?> GetByCompanyAndRoleAsync(
        string companyCode,
        string role,
        CancellationToken cancellationToken = default)
    {
        var normalizedCompanyCode = NormalizeCompanyCode(companyCode);
        var normalizedRole = role.Trim();
        var range = _rangesById.Values.SingleOrDefault(item =>
            item.CompanyCode.Equals(normalizedCompanyCode, StringComparison.OrdinalIgnoreCase)
            && item.Role.Equals(normalizedRole, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(range);
    }

    public Task<IReadOnlyCollection<AccountCodeRange>> ListByCompanyAsync(
        string companyCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedCompanyCode = NormalizeCompanyCode(companyCode);
        IReadOnlyCollection<AccountCodeRange> ranges = _rangesById.Values
            .Where(range => range.CompanyCode.Equals(normalizedCompanyCode, StringComparison.OrdinalIgnoreCase))
            .OrderBy(range => range.Role, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult(ranges);
    }

    private static string NormalizeCompanyCode(string companyCode)
    {
        return companyCode.Trim().ToUpperInvariant();
    }
}
