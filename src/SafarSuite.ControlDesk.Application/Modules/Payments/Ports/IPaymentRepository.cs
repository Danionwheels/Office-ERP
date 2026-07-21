using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.Ports;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);

    Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken = default);

    Task<Payment?> GetByPortalClaimIdAsync(
        PortalPaymentClaimId portalClaimId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Payment>> ListByReferenceAsync(
        PaymentReference reference,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Payment>> ListForClientAsync(
        ClientId clientId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);
}
