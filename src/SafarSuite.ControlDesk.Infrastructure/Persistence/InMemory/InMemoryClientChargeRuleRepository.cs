using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryClientChargeRuleRepository : IClientChargeRuleRepository
{
    private readonly ConcurrentDictionary<Guid, ClientChargeRule> _rulesById = new();

    public Task AddAsync(ClientChargeRule clientChargeRule, CancellationToken cancellationToken = default)
    {
        _rulesById.TryAdd(clientChargeRule.Id.Value, clientChargeRule);

        return Task.CompletedTask;
    }

    public Task<ClientChargeRule?> GetByIdAsync(ClientChargeRuleId id, CancellationToken cancellationToken = default)
    {
        _rulesById.TryGetValue(id.Value, out var clientChargeRule);

        return Task.FromResult(clientChargeRule);
    }

    public Task<IReadOnlyCollection<ClientChargeRule>> ListEffectiveForClientAsync(
        ClientId clientId,
        ContractId? contractId,
        DateOnly billingDate,
        CancellationToken cancellationToken = default)
    {
        var rules = _rulesById.Values
            .Where(rule => rule.ClientId.Equals(clientId))
            .Where(rule => !rule.ContractId.HasValue
                || !contractId.HasValue
                || rule.ContractId.Value.Equals(contractId.Value))
            .Where(rule => rule.IsEffectiveOn(billingDate))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<ClientChargeRule>>(rules);
    }
}
