namespace SafarSuite.ControlCloud.Api.Modules.ClientPortal;

public sealed class ClientPortalProviderAccessOptions
{
    public const string SectionName = "ClientPortal:ProviderAccess";

    public string SharedSecret { get; set; } =
        "local-development-provider-access-secret-change-before-cloud";

    public string SessionSigningSecret { get; set; } =
        "local-development-provider-session-signing-secret-change-before-cloud";

    public int SessionMinutes { get; set; } = 60;

    public string[] DefaultScopes { get; set; } =
    [
        "app-activation:read",
        "app-activation:write",
        "client-portal:manage"
    ];

    public ProviderAccessUserOptions[] Users { get; set; } =
    [
        new ProviderAccessUserOptions
        {
            UserId = "local-provider-admin",
            Email = "provider.admin@safarsuite.local",
            FullName = "Local Provider Admin",
            PasswordHash = "pbkdf2-sha256.120000.AQIDBAUGBwgJCgsMDQ4PEA.bKfX3l_4QOvv59HDi9Wq1UzY3FYjDWr3w5qQgkLufc4",
            Status = "Active",
            Scopes =
            [
                "app-activation:read",
                "app-activation:write",
                "client-portal:manage"
            ]
        }
    ];
}

public sealed class ProviderAccessUserOptions
{
    public string UserId { get; set; } = "";

    public string Email { get; set; } = "";

    public string FullName { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public string Status { get; set; } = "Active";

    public string[] Scopes { get; set; } = [];
}
