using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class ClientPortalMailDeliveryQueue : IClientPortalMailDeliveryQueue
{
    private readonly IClientPortalMailDeliveryRepository _repository;

    public ClientPortalMailDeliveryQueue(IClientPortalMailDeliveryRepository repository)
    {
        _repository = repository;
    }

    public Task EnqueueAsync(
        ClientPortalMailDeliveryRequest request,
        CancellationToken cancellationToken = default)
    {
        var delivery = ControlCloudClientPortalMailDelivery.Create(
            request.DeliveryId,
            request.ClientId,
            request.RecipientEmail,
            request.RecipientName,
            request.Subject,
            request.TextBody,
            request.CreatedAtUtc);

        return _repository.AddAsync(delivery, cancellationToken);
    }
}
