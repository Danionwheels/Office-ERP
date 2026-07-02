using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.Ports;

public interface IClientCreditApplicationRepository
{
    Task AddAsync(ClientCreditApplication application, CancellationToken cancellationToken = default);

    Task<ClientCreditApplication?> GetByIdAsync(
        ClientCreditApplicationId id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ClientCreditApplication>> ListForClientAsync(
        ClientId clientId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByReferenceAsync(
        ClientCreditApplicationReference reference,
        CancellationToken cancellationToken = default);
}
