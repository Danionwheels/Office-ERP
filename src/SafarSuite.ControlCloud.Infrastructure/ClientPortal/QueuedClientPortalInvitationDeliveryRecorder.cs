using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class QueuedClientPortalInvitationDeliveryRecorder : IClientPortalInvitationDeliveryRecorder
{
    private readonly IClientPortalMailDeliveryQueue _mail;
    private readonly ClientPortalInvitationDeliveryOptions _options;

    public QueuedClientPortalInvitationDeliveryRecorder(
        IClientPortalMailDeliveryQueue mail,
        ClientPortalInvitationDeliveryOptions options)
    {
        _mail = mail;
        _options = options;
    }

    public Task RecordAsync(ClientPortalInvitationDeliveryRecord delivery, CancellationToken cancellationToken = default)
    {
        var prefix = string.IsNullOrWhiteSpace(_options.SubjectPrefix) ? "SafarSuite" : _options.SubjectPrefix.Trim();
        var body = string.Join(
            Environment.NewLine,
            [
                $"Hello {(string.IsNullOrWhiteSpace(delivery.FullName) ? delivery.Email : delivery.FullName.Trim())},",
                "",
                "You have been invited to access the SafarSuite Client Portal.",
                $"Role: {delivery.Role}",
                $"Expires at UTC: {delivery.ExpiresAtUtc:O}",
                "",
                "Open this link to set your password and sign in:",
                delivery.InvitationUrl,
                "",
                "If you were not expecting this invitation, you can ignore this email."
            ]);
        return _mail.EnqueueAsync(
            new ClientPortalMailDeliveryRequest(
                delivery.DeliveryId,
                delivery.ClientId,
                delivery.Email,
                delivery.FullName,
                $"{prefix} Client Portal invitation",
                body,
                delivery.RecordedAtUtc),
            cancellationToken);
    }
}
