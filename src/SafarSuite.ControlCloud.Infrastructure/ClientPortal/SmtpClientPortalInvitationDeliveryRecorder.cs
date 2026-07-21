using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class SmtpClientPortalInvitationDeliveryRecorder : IClientPortalInvitationDeliveryRecorder
{
    private readonly IClientPortalMailDeliveryQueue _mailQueue;
    private readonly ClientPortalInvitationDeliveryOptions _options;

    public SmtpClientPortalInvitationDeliveryRecorder(
        IClientPortalMailDeliveryQueue mailQueue,
        ClientPortalInvitationDeliveryOptions options)
    {
        _mailQueue = mailQueue;
        _options = options;
    }

    public Task RecordAsync(
        ClientPortalInvitationDeliveryRecord delivery,
        CancellationToken cancellationToken = default)
    {
        return _mailQueue.EnqueueAsync(
            new ClientPortalMailDeliveryRequest(
                delivery.DeliveryId,
                delivery.ClientId,
                delivery.Email,
                delivery.FullName,
                BuildSubject(),
                BuildBody(delivery),
                delivery.RecordedAtUtc),
            cancellationToken);
    }

    private string BuildSubject()
    {
        var prefix = string.IsNullOrWhiteSpace(_options.SubjectPrefix)
            ? "SafarSuite"
            : _options.SubjectPrefix.Trim();

        return $"{prefix} Client Portal invitation";
    }

    private static string BuildBody(ClientPortalInvitationDeliveryRecord delivery)
    {
        var greetingName = string.IsNullOrWhiteSpace(delivery.FullName)
            ? delivery.Email
            : delivery.FullName.Trim();

        return string.Join(
            Environment.NewLine,
            [
                $"Hello {greetingName},",
                "",
                "You have been invited to access the SafarSuite Client Portal.",
                "",
                $"Role: {delivery.Role}",
                $"Reason: {delivery.DeliveryReason}",
                $"Expires at UTC: {delivery.ExpiresAtUtc:O}",
                "",
                "Open this link to set your password and sign in:",
                delivery.InvitationUrl,
                "",
                "If you were not expecting this invitation, you can ignore this email."
            ]);
    }

}
