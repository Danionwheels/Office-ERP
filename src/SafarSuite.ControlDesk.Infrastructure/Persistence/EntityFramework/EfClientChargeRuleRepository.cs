using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class EfClientChargeRuleRepository : IClientChargeRuleRepository
{
    private readonly ControlDeskDbContext _dbContext;

    public EfClientChargeRuleRepository(ControlDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ClientChargeRule clientChargeRule, CancellationToken cancellationToken = default)
    {
        await _dbContext.ClientChargeRules.AddAsync(clientChargeRule, cancellationToken);
    }

    public async Task<ClientChargeRule?> GetByIdAsync(ClientChargeRuleId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClientChargeRules
            .SingleOrDefaultAsync(rule => rule.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ClientChargeRule>> ListEffectiveForClientAsync(
        ClientId clientId,
        ContractId? contractId,
        DateOnly billingDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClientChargeRules
            .Where(rule => rule.ClientId == clientId)
            .Where(rule => !rule.ContractId.HasValue
                || !contractId.HasValue
                || rule.ContractId.Value == contractId.Value)
            .Where(rule => rule.Status == ClientChargeRuleStatus.Active)
            .Where(rule => rule.EffectivePeriod.StartsOn <= billingDate
                && rule.EffectivePeriod.EndsOn >= billingDate)
            .OrderBy(rule => rule.CreatedAtUtc)
            .ThenBy(rule => rule.Id)
            .ToArrayAsync(cancellationToken);
    }
}
