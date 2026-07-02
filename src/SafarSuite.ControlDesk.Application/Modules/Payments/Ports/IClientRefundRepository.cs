using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.Ports;

public interface IClientRefundRepository
{
    Task AddAsync(ClientRefund refund, CancellationToken cancellationToken = default);

    Task<ClientRefund?> GetByIdAsync(ClientRefundId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ClientRefund>> ListForClientAsync(
        ClientId clientId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByReferenceAsync(
        ClientRefundReference reference,
        CancellationToken cancellationToken = default);
}
