namespace SafarSuite.ControlCloud.Application.Modules.InboundControlDesk.Ports;

public interface IControlCloudSigningKeyStore
{
    bool TryGetSecret(string keyId, out string secret);
}
