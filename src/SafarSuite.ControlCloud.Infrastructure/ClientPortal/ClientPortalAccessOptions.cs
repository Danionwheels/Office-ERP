namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class ClientPortalAccessOptions
{
    public const string SectionName = "ClientPortal:Access";

    public string SessionSigningSecret { get; set; } =
        "local-development-client-portal-session-secret-change-before-cloud";

    public int AccessTokenMinutes { get; set; } = 10;

    public int SessionIdleTimeoutMinutes { get; set; } = 30;

    public int SessionAbsoluteTimeoutMinutes { get; set; } = 720;

    public int RefreshTokenBytes { get; set; } = 32;

    public int PasswordResetTokenBytes { get; set; } = 32;

    public int PasswordResetMinutes { get; set; } = 30;

    public string MfaProtectionSecret { get; set; } =
        "local-development-client-portal-mfa-secret-change-before-cloud";

    public string PublicPortalUrl { get; set; } =
        "http://localhost:5000/client-portal/index.html";

    public int InvitationTokenBytes { get; set; } = 32;

    public string IdentityStorePath { get; set; } = "App_Data/client-portal-identities.json";

    public string SessionStorePath { get; set; } = "App_Data/client-portal-sessions.json";

    public string PasswordResetStorePath { get; set; } = "App_Data/client-portal-password-resets.json";

    public string PaymentClaimStorePath { get; set; } = "App_Data/client-portal-payment-claims.json";

    public string AttachmentStorePath { get; set; } = "App_Data/client-portal-attachments.json";

    public string ProviderBankDetailsStorePath { get; set; } = "App_Data/provider-bank-details.json";
}
