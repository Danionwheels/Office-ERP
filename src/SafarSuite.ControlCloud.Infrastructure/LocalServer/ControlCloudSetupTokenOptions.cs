namespace SafarSuite.ControlCloud.Infrastructure.LocalServer;

public sealed class ControlCloudSetupTokenOptions
{
    public const string SectionName = "ControlCloud:SetupTokens";

    public int TokenBytes { get; set; } = 32;

    public string TokenStorePath { get; set; } =
        "App_Data/control-cloud-installation-setup-tokens.json";
}
