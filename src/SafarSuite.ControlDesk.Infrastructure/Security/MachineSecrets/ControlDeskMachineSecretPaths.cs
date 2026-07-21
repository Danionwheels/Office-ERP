using System.Runtime.Versioning;

namespace SafarSuite.ControlDesk.Infrastructure.Security.MachineSecrets;

public static class ControlDeskMachineSecretPaths
{
    public const string EnvelopeFileName = "control-desk-machine-secrets.v1.json";

    [SupportedOSPlatform("windows")]
    public static string GetCanonicalEnvelopePath()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "The installed Control Desk machine-secret envelope requires Windows.");
        }

        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        if (string.IsNullOrWhiteSpace(programData))
        {
            throw new InvalidOperationException(
                "The Windows common application-data directory is unavailable.");
        }

        return Path.Combine(
            programData,
            "SafarSuite",
            "ControlDesk",
            "Secrets",
            "Machine",
            EnvelopeFileName);
    }
}
