using System.Net;
using System.Net.Mail;
using System.Text;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class SmtpClientPortalInvitationDeliveryRecorder : IClientPortalInvitationDeliveryRecorder
{
    private readonly ClientPortalInvitationDeliveryOptions _options;

    public SmtpClientPortalInvitationDeliveryRecorder(ClientPortalInvitationDeliveryOptions options)
    {
        _options = options;
    }

    public async Task RecordAsync(
        ClientPortalInvitationDeliveryRecord delivery,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SmtpHost))
        {
            throw new InvalidOperationException(
                "ClientPortal:InvitationDelivery:SmtpHost is required when invitation delivery provider is Smtp.");
        }

        if (string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            throw new InvalidOperationException(
                "ClientPortal:InvitationDelivery:FromEmail is required when invitation delivery provider is Smtp.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail.Trim(), NormalizeFromName(_options.FromName)),
            Subject = BuildSubject(),
            SubjectEncoding = Encoding.UTF8,
            Body = BuildBody(delivery),
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(delivery.Email, delivery.FullName));

        using var client = new SmtpClient(
            _options.SmtpHost.Trim(),
            Math.Clamp(_options.SmtpPort, 1, 65535))
        {
            EnableSsl = _options.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            client.Credentials = new NetworkCredential(
                _options.Username.Trim(),
                _options.Password);
        }

        await client.SendMailAsync(message, cancellationToken);
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

    private static string NormalizeFromName(string fromName)
    {
        return string.IsNullOrWhiteSpace(fromName)
            ? "SafarSuite Control Cloud"
            : fromName.Trim();
    }
}
