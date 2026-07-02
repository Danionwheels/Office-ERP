namespace SafarSuite.ControlCloud.Infrastructure.LocalServer;

public sealed class ControlCloudBootstrapPackageOptions
{
    public const string SectionName = "ControlCloud:BootstrapPackages";

    public string CloudBaseUrl { get; set; } = "http://localhost:5199";

    public string InstallScriptUrl { get; set; } = "";
}
