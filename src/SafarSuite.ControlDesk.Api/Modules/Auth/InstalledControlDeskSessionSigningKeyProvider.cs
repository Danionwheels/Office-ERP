using System.Runtime.Versioning;
using SafarSuite.ControlDesk.Infrastructure.Security.MachineSecrets;

namespace SafarSuite.ControlDesk.Api.Modules.Auth;

[SupportedOSPlatform("windows")]
internal sealed class InstalledControlDeskSessionSigningKeyProvider :
    IControlDeskSessionSigningKeyProvider,
    IDisposable
{
    private readonly Lazy<ControlDeskMachineSecretConfigurationProvider> _provider;

    public InstalledControlDeskSessionSigningKeyProvider()
    {
        _provider = new(ControlDeskMachineSecretConfigurationProvider.LoadInstalled);
    }

    public string SessionSigningKeyId => _provider.Value.SessionSigningKeyId;

    public byte[] CopySessionSigningKey() => _provider.Value.CopySessionSigningKey();

    public void Dispose()
    {
        if (_provider.IsValueCreated)
        {
            _provider.Value.Dispose();
        }
    }
}
