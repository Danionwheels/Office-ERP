using System.Net;
using System.Net.Mail;
using System.Text;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class SmtpClientPortalMailTransport : IClientPortalMailTransport
{
    private readonly ClientPortalInvitationDeliveryOptions _options;

    public SmtpClientPortalMailTransport(ClientPortalInvitationDeliveryOptions options)
    {
        _options = options;
    }

    public async Task SendAsync(
        ClientPortalMailMessage message,
        CancellationToken cancellationToken = default)
    {
        ValidateOptions();

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(_options.FromEmail.Trim(), NormalizeFromName(_options.FromName)),
            Subject = message.Subject,
            SubjectEncoding = Encoding.UTF8,
            Body = message.TextBody,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = false
        };
        mailMessage.To.Add(new MailAddress(message.RecipientEmail, message.RecipientName));
        mailMessage.Headers.Add("X-SafarSuite-Delivery-Id", message.DeliveryId.ToString("D"));

        using var client = new SmtpClient(
            _options.SmtpHost.Trim(),
            Math.Clamp(_options.SmtpPort, 1, 65_535))
        {
            EnableSsl = _options.EnableSsl,
            Timeout = Math.Clamp(_options.SmtpTimeoutSeconds, 1, 300) * 1_000
        };

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            client.Credentials = new NetworkCredential(
                _options.Username.Trim(),
                _options.Password);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.SmtpTimeoutSeconds, 1, 300)));
        await client.SendMailAsync(mailMessage, timeout.Token);
    }

    private void ValidateOptions()
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
    }

    private static string NormalizeFromName(string fromName)
    {
        return string.IsNullOrWhiteSpace(fromName)
            ? "SafarSuite Control Cloud"
            : fromName.Trim();
    }
}
