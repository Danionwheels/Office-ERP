using SafarSuite.ControlCloud.Domain.Modules.InboundControlDesk;

namespace SafarSuite.ControlCloud.Application.Modules.InboundControlDesk.Ports;

public interface IControlDeskEnvelopeReceiptRepository
{
    Task<ControlDeskEnvelopeReceipt?> GetAcceptedByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ControlDeskEnvelopeReceipt receipt,
        CancellationToken cancellationToken = default);
}
