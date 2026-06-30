using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.Ports;

public interface IClientChargeRuleRepository
{
    Task AddAsync(ClientChargeRule clientChargeRule, CancellationToken cancellationToken = default);

    Task<ClientChargeRule?> GetByIdAsync(ClientChargeRuleId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ClientChargeRule>> ListEffectiveForClientAsync(
        ClientId clientId,
        ContractId? contractId,
        DateOnly billingDate,
        CancellationToken cancellationToken = default);
}
