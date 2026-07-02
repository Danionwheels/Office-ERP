namespace SafarSuite.LocalServer.Infrastructure.Registration;

public sealed class LocalServerBootstrapTrustOptions
{
    public const string SectionName = "LocalServer:BootstrapTrust";

    public string ConfigurationStorePath { get; set; } =
        "App_Data/local-server-bootstrap-configuration.json";

    public IReadOnlyCollection<LocalServerBootstrapTrustKeyOptions> SigningKeys { get; set; } =
    [
        new LocalServerBootstrapTrustKeyOptions
        {
            KeyId = "local-entitlement-dev",
            Secret = "local-entitlement-signing-secret-change-before-cloud"
        }
    ];
}

public sealed class LocalServerBootstrapTrustKeyOptions
{
    public string KeyId { get; set; } = "";

    public string Secret { get; set; } = "";
}
