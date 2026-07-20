using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace SafarSuite.ControlDesk.Infrastructure.Security.MachineSecrets;

[SupportedOSPlatform("windows")]
public sealed class ControlDeskMachineSecretConfigurationProvider : IDisposable
{
    private byte[]? _sessionSigningKey;

    private ControlDeskMachineSecretConfigurationProvider(
        string sessionSigningKeyId,
        byte[] sessionSigningKey)
    {
        SessionSigningKeyId = sessionSigningKeyId;
        _sessionSigningKey = sessionSigningKey;
    }

    public string SessionSigningKeyId { get; }

    public static ControlDeskMachineSecretConfigurationProvider LoadInstalled()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "The installed Control Desk machine-secret provider requires Windows.");
        }

        var store = new ControlDeskMachineSecretEnvelopeStore(
            ControlDeskMachineSecretPaths.GetCanonicalEnvelopePath(),
            ControlDeskMachineSecretAccessProfile.InstalledApiService);

        using var snapshot = store.Read();
        return new ControlDeskMachineSecretConfigurationProvider(
            snapshot.SessionSigningKeyId,
            snapshot.CopySessionSigningKey());
    }

    public byte[] CopySessionSigningKey()
    {
        var key = _sessionSigningKey;

        if (key is null)
        {
            throw new ObjectDisposedException(nameof(ControlDeskMachineSecretConfigurationProvider));
        }

        return key.ToArray();
    }

    public void Dispose()
    {
        var key = Interlocked.Exchange(ref _sessionSigningKey, null);

        if (key is not null)
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }
}
