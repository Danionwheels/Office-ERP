namespace SafarSuite.ControlCloud.Api.Modules.ClientPortal;

public sealed class ClientPortalProviderAccessOptions
{
    public const string SectionName = "ClientPortal:ProviderAccess";

    public string SharedSecret { get; set; } =
        "local-development-provider-access-secret-change-before-cloud";
}
