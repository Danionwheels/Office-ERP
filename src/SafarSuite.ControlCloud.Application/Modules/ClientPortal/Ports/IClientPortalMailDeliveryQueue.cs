namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

public interface IClientPortalMailDeliveryQueue
{
    Task EnqueueAsync(
        ClientPortalMailDeliveryRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record ClientPortalMailDeliveryRequest(
    Guid DeliveryId,
    Guid? ClientId,
    string RecipientEmail,
    string RecipientName,
    string Subject,
    string TextBody,
    DateTimeOffset CreatedAtUtc);
