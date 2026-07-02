namespace SafarSuite.ControlCloud.Infrastructure.LocalServer;

public sealed class ControlCloudDiagnosticsOptions
{
    public const string SectionName = "ControlCloud:Diagnostics";

    public string DiagnosticStorePath { get; set; } =
        "App_Data/control-cloud-installation-diagnostics.json";
}
