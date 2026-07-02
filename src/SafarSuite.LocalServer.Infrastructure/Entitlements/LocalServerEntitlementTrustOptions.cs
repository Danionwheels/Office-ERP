namespace SafarSuite.LocalServer.Infrastructure.Entitlements;

public sealed class LocalServerEntitlementTrustOptions
{
    public const string SectionName = "LocalServer:EntitlementTrust";

    public string ExpectedIssuer { get; set; } = "SafarSuite.ControlCloud";

    public string ExpectedAudience { get; set; } = "SafarSuite.ClientPortal";

    public string CacheStorePath { get; set; } =
        "App_Data/local-server-entitlement-cache.json";

    public string TrustStateStorePath { get; set; } =
        "App_Data/local-server-entitlement-trust-state.json";

    public string ImportAuditStorePath { get; set; } =
        "App_Data/local-server-entitlement-import-audit.json";

    public int MaxImportAuditRecords { get; set; } = 500;

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
