using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryOpeningBalanceProfileRepository : IOpeningBalanceProfileRepository
{
    private readonly ConcurrentDictionary<string, OpeningBalanceProfile> _profilesByCompany = new(
        StringComparer.OrdinalIgnoreCase);

    public Task AddAsync(OpeningBalanceProfile profile, CancellationToken cancellationToken = default)
    {
        _profilesByCompany.TryAdd(profile.CompanyCode, profile);

        return Task.CompletedTask;
    }

    public Task<OpeningBalanceProfile?> GetByCompanyAsync(
        string companyCode,
        CancellationToken cancellationToken = default)
    {
        _profilesByCompany.TryGetValue(NormalizeCompanyCode(companyCode), out var profile);

        return Task.FromResult(profile);
    }

    private static string NormalizeCompanyCode(string companyCode)
    {
        return companyCode.Trim().ToUpperInvariant();
    }
}
