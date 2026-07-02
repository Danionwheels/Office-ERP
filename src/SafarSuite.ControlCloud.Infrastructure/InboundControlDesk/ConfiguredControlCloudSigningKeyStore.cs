using SafarSuite.ControlCloud.Application.Modules.InboundControlDesk.Ports;

namespace SafarSuite.ControlCloud.Infrastructure.InboundControlDesk;

public sealed class ConfiguredControlCloudSigningKeyStore : IControlCloudSigningKeyStore
{
    private readonly IReadOnlyDictionary<string, string> _secretsByKeyId;

    public ConfiguredControlCloudSigningKeyStore(ControlCloudReceiverOptions options)
    {
        _secretsByKeyId = options.SigningKeys
            .Where(key => !string.IsNullOrWhiteSpace(key.KeyId) && !string.IsNullOrWhiteSpace(key.Secret))
            .GroupBy(key => key.KeyId.Trim(), StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.First().Secret.Trim(),
                StringComparer.Ordinal);
    }

    public bool TryGetSecret(string keyId, out string secret)
    {
        return _secretsByKeyId.TryGetValue(keyId.Trim(), out secret!);
    }
}
