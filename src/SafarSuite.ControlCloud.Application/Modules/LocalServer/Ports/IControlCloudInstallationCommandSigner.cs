using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;

public interface IControlCloudInstallationCommandSigner
{
    ControlCloudInstallationCommandSignature Sign(
        ControlCloudInstallationCommandSigningPayload payload);
}
