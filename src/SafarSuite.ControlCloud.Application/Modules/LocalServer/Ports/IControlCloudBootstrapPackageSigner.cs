using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;

public interface IControlCloudBootstrapPackageSigner
{
    string SigningKeyId { get; }

    ControlCloudBootstrapSecretReadiness GetSecretReadiness();

    ControlCloudSignedBootstrapPackage Sign(ControlCloudBootstrapPackagePayload payload);

    ControlCloudBootstrapPackageSignature SignPayloadJson(string payloadJson);
}
