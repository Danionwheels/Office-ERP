namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class ClientPortalInvitationDeliveryOptions
{
    public const string SectionName = "ClientPortal:InvitationDelivery";

    public string Provider { get; set; } = "File";

    public string StorePath { get; set; } = "App_Data/client-portal-invitation-deliveries.jsonl";

    public string FromEmail { get; set; } = "no-reply@safarsuite.local";

    public string FromName { get; set; } = "SafarSuite Control Cloud";

    public string SubjectPrefix { get; set; } = "SafarSuite";

    public string SmtpHost { get; set; } = "";

    public int SmtpPort { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string Username { get; set; } = "";

    public string Password { get; set; } = "";

    public int SmtpTimeoutSeconds { get; set; } = 30;

    public string MailQueueStorePath { get; set; } = "App_Data/client-portal-mail-deliveries.json";

    public int MailQueuePollIntervalSeconds { get; set; } = 10;

    public int MailQueueClaimLeaseSeconds { get; set; } = 120;

    public int MailQueueBatchSize { get; set; } = 1;

    public int MailQueueInitialRetryDelaySeconds { get; set; } = 30;
}
