namespace SafarSuite.LocalServer.Infrastructure.Pairing;

public sealed class LocalServerDeviceCredentialOptions
{
    public const string SectionName = "DeviceCredentials";

    public string SigningKeyId { get; set; } = "safarsuite-app-device-local";

    public string SigningSecret { get; set; } = string.Empty;

    public int ExpiresInDays { get; set; } = 3650;
}
