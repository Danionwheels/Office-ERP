using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;

public interface IControlCloudFirstManagerSetupTokenSigner
{
    string SigningKeyId { get; }

    LocalServerSignedFirstManagerSetupTokenResponse Sign(
        LocalServerFirstManagerSetupTokenPayloadResponse payload);
}
