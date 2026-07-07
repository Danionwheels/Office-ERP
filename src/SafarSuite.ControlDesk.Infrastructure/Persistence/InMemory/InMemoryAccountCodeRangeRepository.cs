using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryAccountCodeRangeRepository : IAccountCodeRangeRepository
{
    private readonly ConcurrentDictionary<Guid, AccountCodeRange> _rangesById = new();
    private readonly object _gate = new();

    public Task AddAsync(AccountCodeRange range, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_rangesById.Values.Any(item => HasCompanyAndRole(item, range.CompanyCode, range.Role)))
            {
                return Task.CompletedTask;
            }

            _rangesById.TryAdd(range.Id.Value, range);
        }

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
        var range = _rangesById.Values
            .Where(item => HasCompanyAndRole(item, normalizedCompanyCode, normalizedRole))
            .OrderBy(item => item.CreatedAtUtc)
            .ThenBy(item => item.Id.Value)
            .FirstOrDefault();

        return Task.FromResult(range);
    }

    public Task<IReadOnlyCollection<AccountCodeRange>> ListByCompanyAsync(
        string companyCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedCompanyCode = NormalizeCompanyCode(companyCode);
        IReadOnlyCollection<AccountCodeRange> ranges = _rangesById.Values
            .Where(range => range.CompanyCode.Equals(normalizedCompanyCode, StringComparison.OrdinalIgnoreCase))
            .GroupBy(range => range.Role, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderBy(range => range.CreatedAtUtc)
                .ThenBy(range => range.Id.Value)
                .First())
            .OrderBy(range => range.Role, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult(ranges);
    }

    private static bool HasCompanyAndRole(AccountCodeRange range, string companyCode, string role)
    {
        return range.CompanyCode.Equals(NormalizeCompanyCode(companyCode), StringComparison.OrdinalIgnoreCase)
            && range.Role.Equals(role.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeCompanyCode(string companyCode)
    {
        return companyCode.Trim().ToUpperInvariant();
    }
}
