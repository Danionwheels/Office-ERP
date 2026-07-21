namespace SafarSuite.ControlCloud.Infrastructure.LocalServer;

public sealed class ControlCloudFirstManagerSetupTokenOptions
{
    public const string SectionName = "ControlCloud:FirstManagerSetupTokens";

    public string IssueStorePath { get; set; } = "App_Data/control-cloud-first-manager-setup-token-issues.json";
}
