namespace SafarSuite.ControlDesk.Infrastructure.ControlCloud;

public sealed class ControlCloudPortalInvitationOptions
{
    public const string SectionName = "ControlCloud:PortalInvitations";

    public string BaseUrl { get; set; } = "http://localhost:5127";

    public string ProviderAccessSecret { get; set; } = string.Empty;

    public string ProviderAccessToken { get; set; } = "";
}
