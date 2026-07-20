using System.Security.Cryptography;
using System.Text;

namespace SafarSuite.ControlDesk.Api.Modules.Auth;

internal sealed class ConfiguredControlDeskSessionSigningKeyProvider(
    IConfiguration configuration) : IControlDeskSessionSigningKeyProvider
{
    public const string DefaultKeyId = "configured-session-signing-key-v1";

    public string SessionSigningKeyId =>
        configuration[$"{ControlDeskOperatorAccessOptions.SectionName}:SessionSigningKeyId"]?.Trim()
        ?? DefaultKeyId;

    public byte[] CopySessionSigningKey()
    {
        var secret = configuration[
            $"{ControlDeskOperatorAccessOptions.SectionName}:SessionSigningSecret"]?.Trim()
            ?? string.Empty;

        return Encoding.UTF8.GetBytes(secret);
    }
}
