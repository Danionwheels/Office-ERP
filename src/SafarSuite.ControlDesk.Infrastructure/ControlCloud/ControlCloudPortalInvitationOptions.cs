namespace SafarSuite.ControlDesk.Infrastructure.ControlCloud;

public sealed class ControlCloudPortalInvitationOptions
{
    public const string SectionName = "ControlCloud:PortalInvitations";

    public string BaseUrl { get; set; } = "http://localhost:5127";

    public string ProviderAccessSecret { get; set; } =
        "local-development-provider-access-secret-change-before-cloud";
}
