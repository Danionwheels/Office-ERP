namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

public interface IClientPortalMailTransport
{
    Task SendAsync(
        ClientPortalMailMessage message,
        CancellationToken cancellationToken = default);
}

public sealed record ClientPortalMailMessage(
    Guid DeliveryId,
    Guid? ClientId,
    string RecipientEmail,
    string RecipientName,
    string Subject,
    string TextBody);
