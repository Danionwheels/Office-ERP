namespace SafarSuite.LocalServer.Infrastructure.Entitlements;

public sealed class LocalServerEntitlementTrustOptions
{
    public const string SectionName = "LocalServer:EntitlementTrust";

    public string ExpectedIssuer { get; set; } = "SafarSuite.ControlCloud";

    public string ExpectedAudience { get; set; } = "SafarSuite.ClientPortal";

    public string CacheStorePath { get; set; } =
        "App_Data/local-server-entitlement-cache.json";

    public IReadOnlyCollection<LocalServerEntitlementTrustKeyOptions> SigningKeys { get; set; } =
    [
        new LocalServerEntitlementTrustKeyOptions
        {
            KeyId = "local-entitlement-dev",
            Secret = "local-entitlement-signing-secret-change-before-cloud"
        }
    ];
}

public sealed class LocalServerEntitlementTrustKeyOptions
{
    public string KeyId { get; set; } = "";

    public string Secret { get; set; } = "";
}
