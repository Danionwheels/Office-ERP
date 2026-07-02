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
}
