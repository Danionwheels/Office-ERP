namespace SafarSuite.LocalServer.Infrastructure.Pairing;

public sealed class LocalServerPairingStoreOptions
{
    public const string SectionName = "LocalServer:Pairing";

    public string DeviceStorePath { get; set; } =
        "App_Data/local-server-device-pairings.json";
}
