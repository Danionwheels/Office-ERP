using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryContractRepository : IContractRepository, IProductModuleReferenceReader
{
    private readonly object _sync = new();
    private readonly ConcurrentDictionary<Guid, ClientContract> _contractsById = new();

    public Task AddAsync(ClientContract contract, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (_contractsById.ContainsKey(contract.Id.Value))
            {
                throw new InvalidOperationException("Contract revision already exists.");
            }

            var latest = FindLatest(contract.ClientId);

            if (latest is null && contract.SupersedesContractId is not null)
            {
                throw new InvalidOperationException("The first contract revision cannot supersede another revision.");
            }

            if (latest is not null && contract.SupersedesContractId != latest.Id)
            {
                throw new InvalidOperationException("Contract revision must supersede the current latest revision.");
            }

            if (latest is not null && contract.RevisionNumber != latest.RevisionNumber + 1)
            {
                throw new InvalidOperationException("Contract revision number must increase by one.");
            }

            _contractsById.TryAdd(contract.Id.Value, contract);
        }

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

    public Task<ClientContract?> GetLatestForClientForUpdateAsync(
        ClientId clientId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            return Task.FromResult(FindLatest(clientId));
        }
    }

    public Task<IReadOnlyCollection<ClientContract>> ListForClientAsync(
        ClientId clientId,
        CancellationToken cancellationToken = default)
    {
        var contracts = _contractsById.Values
            .Where(contract => contract.ClientId == clientId)
            .OrderByDescending(contract => contract.RevisionNumber)
            .ThenByDescending(contract => contract.ApprovedAtUtc)
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

    public Task<IReadOnlyCollection<ProductModuleContractReference>> ListActiveAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var references = _contractsById.Values
            .Where(contract => contract.Status == ContractStatus.Active)
            .SelectMany(contract => contract.ModuleAllowances
                .Where(module => module.IsEnabled)
                .Select(module => new ProductModuleContractReference(
                    module.ModuleCode.Value,
                    contract.Id.Value,
                    contract.Number.Value,
                    contract.RevisionNumber,
                    contract.ClientId.Value)))
            .OrderBy(reference => reference.ContractNumber, StringComparer.OrdinalIgnoreCase)
            .ThenBy(reference => reference.ContractRevisionNumber)
            .ThenBy(reference => reference.ContractId)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<ProductModuleContractReference>>(references);
    }

    private ClientContract? FindLatest(ClientId clientId)
    {
        return _contractsById.Values
            .Where(contract => contract.ClientId == clientId)
            .OrderByDescending(contract => contract.RevisionNumber)
            .ThenByDescending(contract => contract.ApprovedAtUtc)
            .ThenByDescending(contract => contract.Id.Value)
            .FirstOrDefault();
    }
}
