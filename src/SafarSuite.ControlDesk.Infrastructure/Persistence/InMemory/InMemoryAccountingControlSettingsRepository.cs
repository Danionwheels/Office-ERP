using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryAccountingControlSettingsRepository : IAccountingControlSettingsRepository
{
    private readonly ConcurrentDictionary<string, AccountingControlSettings> _settingsByCompany = new(
        StringComparer.OrdinalIgnoreCase);

    public Task AddAsync(AccountingControlSettings settings, CancellationToken cancellationToken = default)
    {
        _settingsByCompany.TryAdd(settings.CompanyCode, settings);

        return Task.CompletedTask;
    }

    public Task<AccountingControlSettings?> GetByCompanyAsync(
        string companyCode,
        CancellationToken cancellationToken = default)
    {
        _settingsByCompany.TryGetValue(NormalizeCompanyCode(companyCode), out var settings);

        return Task.FromResult(settings);
    }

    private static string NormalizeCompanyCode(string companyCode)
    {
        return companyCode.Trim().ToUpperInvariant();
    }
}
