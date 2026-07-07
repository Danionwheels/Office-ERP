using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryVoucherNumberingRuleRepository : IVoucherNumberingRuleRepository
{
    private readonly ConcurrentDictionary<string, VoucherNumberingRule> _rules = new(
        StringComparer.OrdinalIgnoreCase);

    public Task AddAsync(VoucherNumberingRule rule, CancellationToken cancellationToken = default)
    {
        _rules[Key(rule.CompanyCode, rule.SourceType)] = rule;

        return Task.CompletedTask;
    }

    public Task<VoucherNumberingRule?> GetByCompanyAndSourceTypeAsync(
        string companyCode,
        JournalSourceType sourceType,
        CancellationToken cancellationToken = default)
    {
        _rules.TryGetValue(Key(companyCode, sourceType), out var rule);

        return Task.FromResult(rule);
    }

    public Task<IReadOnlyCollection<VoucherNumberingRule>> ListByCompanyAsync(
        string companyCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedCompanyCode = NormalizeCompanyCode(companyCode);
        var rules = _rules.Values
            .Where(rule => string.Equals(rule.CompanyCode, normalizedCompanyCode, StringComparison.OrdinalIgnoreCase))
            .OrderBy(rule => rule.SourceType)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<VoucherNumberingRule>>(rules);
    }

    private static string Key(string companyCode, JournalSourceType sourceType)
    {
        return $"{NormalizeCompanyCode(companyCode)}:{sourceType}";
    }

    private static string NormalizeCompanyCode(string companyCode)
    {
        return companyCode.Trim().ToUpperInvariant();
    }
}
