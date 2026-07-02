namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class ClientPortalAuditOptions
{
    public const string SectionName = "ClientPortal:Audit";

    public bool Enabled { get; set; } = true;

    public string StorePath { get; set; } = "App_Data/client-portal-audit-events.jsonl";
}
