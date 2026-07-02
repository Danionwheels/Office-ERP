namespace SafarSuite.ControlCloud.Infrastructure.LocalServer;

public sealed class ControlCloudCommandQueueOptions
{
    public const string SectionName = "ControlCloud:CommandQueue";

    public string CommandStorePath { get; set; } =
        "App_Data/control-cloud-installation-commands.json";

    public string AcknowledgementStorePath { get; set; } =
        "App_Data/control-cloud-installation-command-acknowledgements.json";

    public string HeartbeatStorePath { get; set; } =
        "App_Data/control-cloud-installation-heartbeats.json";
}
