using System.Net.Mail;
using System.Text;
using SafarSuite.ControlCloud.Infrastructure.ClientPortal;

namespace SafarSuite.ControlCloud.Api.Modules.ClientPortal;

public static class ClientPortalProductionConfigurationValidator
{
    private static readonly string[] PlaceholderMarkers =
    [
        "local-development",
        "change-before",
        "change-me",
        "replace-with",
        "must-match",
        "placeholder",
        "example-"
    ];

    public static void Validate(
        IHostEnvironment environment,
        IConfiguration configuration,
        ClientPortalAccessOptions accessOptions,
        ClientPortalInvitationDeliveryOptions mailOptions,
        ClientPortalProviderAccessOptions providerOptions)
    {
        ValidatePublicPortalUrl(environment, accessOptions.PublicPortalUrl);
        ValidateMail(environment, mailOptions);

        if (environment.IsDevelopment())
        {
            return;
        }

        ValidateProductionSecret(
            accessOptions.SessionSigningSecret,
            "ClientPortal__Access__SessionSigningSecret");
        ValidateProductionSecret(
            accessOptions.MfaProtectionSecret,
            "ClientPortal__Access__MfaProtectionSecret");
        ValidateProductionSecret(
            providerOptions.SharedSecret,
            "ClientPortal__ProviderAccess__SharedSecret");
        ValidateProductionSecret(
            providerOptions.SessionSigningSecret,
            "ClientPortal__ProviderAccess__SessionSigningSecret");
        ValidateProductionSecret(
            providerOptions.TotpProtectionSecret,
            "ClientPortal__ProviderAccess__TotpProtectionSecret");

        var securitySecrets = new[]
        {
            accessOptions.SessionSigningSecret.Trim(),
            accessOptions.MfaProtectionSecret.Trim(),
            providerOptions.SharedSecret.Trim(),
            providerOptions.SessionSigningSecret.Trim(),
            providerOptions.TotpProtectionSecret.Trim()
        };

        if (securitySecrets.Distinct(StringComparer.Ordinal).Count() != securitySecrets.Length)
        {
            throw new InvalidOperationException(
                "Client Portal and provider access signing, MFA protection, and shared secrets must all be different.");
        }

        if ((providerOptions.Users ?? []).Any(user =>
                string.Equals(user.UserId, "local-provider-admin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(user.Email, "provider.admin@safarsuite.local", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "The built-in local provider seed account cannot be enabled outside Development. Configure production provider users explicitly.");
        }

        var persistenceProvider = configuration.GetValue<string>("Persistence:Provider")?.Trim();

        if (!string.Equals(persistenceProvider, "Postgres", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Persistence__Provider must be Postgres outside Development; Client Portal identity, session, reset, and mail state cannot use File persistence.");
        }
    }

    private static void ValidatePublicPortalUrl(
        IHostEnvironment environment,
        string configuredUrl)
    {
        if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            || string.IsNullOrWhiteSpace(uri.Host)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new InvalidOperationException(
                "CLIENT_PORTAL_PUBLIC_URL must be an absolute HTTP(S) Client Portal page URL without credentials, query parameters, or a fragment.");
        }

        if (!environment.IsDevelopment()
            && (uri.Scheme != Uri.UriSchemeHttps || uri.IsLoopback))
        {
            throw new InvalidOperationException(
                "CLIENT_PORTAL_PUBLIC_URL must be a non-loopback HTTPS URL outside Development.");
        }
    }

    private static void ValidateMail(
        IHostEnvironment environment,
        ClientPortalInvitationDeliveryOptions options)
    {
        if (!options.Provider.Equals("File", StringComparison.OrdinalIgnoreCase)
            && !options.Provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unsupported ClientPortal:InvitationDelivery:Provider '{options.Provider}'. Use 'File' or 'Smtp'.");
        }

        if (!environment.IsDevelopment()
            && !options.Provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Client Portal mail delivery must use SMTP outside Development; File delivery is development-only.");
        }

        if (options.MailQueueBatchSize is < 1 or > 500
            || options.MailQueuePollIntervalSeconds is < 1 or > 300
            || options.MailQueueClaimLeaseSeconds is < 30 or > 3_600
            || options.MailQueueInitialRetryDelaySeconds is < 1 or > 86_400
            || options.SmtpTimeoutSeconds is < 1 or > 300)
        {
            throw new InvalidOperationException(
                "Client Portal mail queue timing, batch, or SMTP timeout settings are outside their supported ranges.");
        }

        if (!options.Provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var smtpHost = options.SmtpHost.Trim();

        if (smtpHost.Length == 0 || Uri.CheckHostName(smtpHost) == UriHostNameType.Unknown)
        {
            throw new InvalidOperationException(
                "ClientPortal__InvitationDelivery__SmtpHost must be a valid SMTP host name or IP address.");
        }

        if (options.SmtpPort is < 1 or > 65_535)
        {
            throw new InvalidOperationException(
                "ClientPortal__InvitationDelivery__SmtpPort must be between 1 and 65535.");
        }

        ValidateFromEmail(options.FromEmail);

        var hasUsername = !string.IsNullOrWhiteSpace(options.Username);
        var hasPassword = !string.IsNullOrWhiteSpace(options.Password);

        if (hasUsername != hasPassword)
        {
            throw new InvalidOperationException(
                "Client Portal SMTP username and password must either both be configured or both be omitted for an anonymous relay.");
        }

        if (!environment.IsDevelopment() && !options.EnableSsl)
        {
            throw new InvalidOperationException(
                "ClientPortal__InvitationDelivery__EnableSsl must be true outside Development.");
        }

        var requiredLeaseSeconds =
            (long)options.MailQueueBatchSize * options.SmtpTimeoutSeconds + 15L;

        if (options.MailQueueClaimLeaseSeconds < requiredLeaseSeconds)
        {
            throw new InvalidOperationException(
                "Client Portal mail claim lease must cover the configured sequential batch and SMTP timeout. Increase MailQueueClaimLeaseSeconds or reduce MailQueueBatchSize.");
        }
    }

    private static void ValidateFromEmail(string configuredEmail)
    {
        var normalized = configuredEmail.Trim();

        try
        {
            var address = new MailAddress(normalized);

            if (!string.Equals(address.Address, normalized, StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException();
            }
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "ClientPortal__InvitationDelivery__FromEmail must contain one valid mailbox address.");
        }
    }

    private static void ValidateProductionSecret(string value, string settingName)
    {
        var normalized = value?.Trim() ?? "";
        var isPlaceholder = PlaceholderMarkers.Any(marker =>
            normalized.Contains(marker, StringComparison.OrdinalIgnoreCase));
        var distinctCharacters = normalized.Distinct().Count();

        if (Encoding.UTF8.GetByteCount(normalized) < 32
            || distinctCharacters < 8
            || isPlaceholder)
        {
            throw new InvalidOperationException(
                $"{settingName} must be a unique, non-placeholder secret with at least 32 UTF-8 bytes and sufficient character diversity outside Development.");
        }
    }
}
