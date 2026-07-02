using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryClientCreditApplicationRepository : IClientCreditApplicationRepository
{
    private readonly ConcurrentDictionary<Guid, ClientCreditApplication> _applicationsById = new();

    public Task AddAsync(
        ClientCreditApplication application,
        CancellationToken cancellationToken = default)
    {
        _applicationsById.TryAdd(application.Id.Value, application);

        return Task.CompletedTask;
    }

    public Task<ClientCreditApplication?> GetByIdAsync(
        ClientCreditApplicationId id,
        CancellationToken cancellationToken = default)
    {
        _applicationsById.TryGetValue(id.Value, out var application);

        return Task.FromResult(application);
    }

    public Task<IReadOnlyCollection<ClientCreditApplication>> ListForClientAsync(
        ClientId clientId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var applications = _applicationsById.Values
            .Where(application => application.ClientId == clientId);

        if (fromDate.HasValue)
        {
            applications = applications.Where(application => application.AppliedOn >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            applications = applications.Where(application => application.AppliedOn <= toDate.Value);
        }

        var sortedApplications = applications
            .OrderBy(application => application.AppliedOn)
            .ThenBy(application => application.CreatedAtUtc)
            .ThenBy(application => application.Id.Value)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<ClientCreditApplication>>(sortedApplications);
    }

    public Task<bool> ExistsByReferenceAsync(
        ClientCreditApplicationReference reference,
        CancellationToken cancellationToken = default)
    {
        var exists = _applicationsById.Values
            .Any(application => string.Equals(
                application.Reference.Value,
                reference.Value,
                StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(exists);
    }
}
