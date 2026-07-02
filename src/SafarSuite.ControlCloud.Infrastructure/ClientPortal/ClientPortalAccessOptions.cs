namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class ClientPortalAccessOptions
{
    public const string SectionName = "ClientPortal:Access";

    public string SessionSigningSecret { get; set; } =
        "local-development-client-portal-session-secret-change-before-cloud";

    public int SessionMinutes { get; set; } = 60;

    public int InvitationTokenBytes { get; set; } = 32;

    public string IdentityStorePath { get; set; } = "App_Data/client-portal-identities.json";
}
