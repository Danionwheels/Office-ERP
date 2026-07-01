using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryContractRepository : IContractRepository
{
    private readonly ConcurrentDictionary<Guid, ClientContract> _contractsById = new();

    public Task AddAsync(ClientContract contract, CancellationToken cancellationToken = default)
    {
        _contractsById.TryAdd(contract.Id.Value, contract);

        return Task.CompletedTask;
    }

    public Task<ClientContract?> GetByIdAsync(ContractId id, CancellationToken cancellationToken = default)
    {
        _contractsById.TryGetValue(id.Value, out var contract);

        return Task.FromResult(contract);
    }

    public Task<ClientContract?> GetActiveForClientAsync(
        ClientId clientId,
        CancellationToken cancellationToken = default)
    {
        var contract = _contractsById.Values
            .Where(contract => contract.ClientId == clientId)
            .Where(contract => contract.Status == ContractStatus.Active)
            .OrderByDescending(contract => contract.ActivatedAtUtc)
            .ThenByDescending(contract => contract.Id.Value)
            .FirstOrDefault();

        return Task.FromResult(contract);
    }

    public Task<IReadOnlyCollection<ClientContract>> ListForClientAsync(
        ClientId clientId,
        CancellationToken cancellationToken = default)
    {
        var contracts = _contractsById.Values
            .Where(contract => contract.ClientId == clientId)
            .OrderByDescending(contract => contract.ActivatedAtUtc)
            .ThenByDescending(contract => contract.CreatedAtUtc)
            .ThenByDescending(contract => contract.Id.Value)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<ClientContract>>(contracts);
    }

    public Task<bool> ExistsByNumberAsync(
        ContractNumber number,
        CancellationToken cancellationToken = default)
    {
        var exists = _contractsById.Values.Any(contract => contract.Number.Equals(number));

        return Task.FromResult(exists);
    }
}
