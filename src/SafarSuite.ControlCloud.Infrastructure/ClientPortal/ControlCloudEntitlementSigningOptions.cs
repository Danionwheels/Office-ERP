namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class ControlCloudEntitlementSigningOptions
{
    public const string SectionName = "ControlCloud:EntitlementSigning";

    public string Issuer { get; set; } = "SafarSuite.ControlCloud";

    public string Audience { get; set; } = "SafarSuite.ClientPortal";

    public string ActiveKeyId { get; set; } = "local-entitlement-dev";

    public string InstallationStorePath { get; set; } = "App_Data/control-cloud-client-installations.json";

    public string BundleIssueStorePath { get; set; } = "App_Data/control-cloud-entitlement-bundle-issues.json";

    public IReadOnlyCollection<ControlCloudEntitlementSigningKeyOptions> SigningKeys { get; set; } =
        Array.Empty<ControlCloudEntitlementSigningKeyOptions>();
}

public sealed class ControlCloudEntitlementSigningKeyOptions
{
    public string KeyId { get; set; } = "";

    public string Secret { get; set; } = "";
}
