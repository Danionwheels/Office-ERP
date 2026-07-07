namespace SafarSuite.LocalServer.Infrastructure.Commands;

public sealed class LocalServerCommandOptions
{
    public const string SectionName = "LocalServer:Commands";

    public string AppActivationRevocationStorePath { get; set; } =
        "App_Data/local-server-app-activation-revocations.json";
}
