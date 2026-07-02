namespace SafarSuite.LocalServer.Infrastructure.Entitlements;

public sealed class ControlCloudEntitlementPullOptions
{
    public const string SectionName = "LocalServer:ControlCloud";

    public Uri BaseUrl { get; set; } = new("http://localhost:5127");
}
