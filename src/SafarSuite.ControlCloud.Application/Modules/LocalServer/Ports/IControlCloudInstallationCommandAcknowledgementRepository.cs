using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;

public interface IControlCloudInstallationCommandAcknowledgementRepository
{
    Task AddAsync(
        ControlCloudInstallationCommandAcknowledgement acknowledgement,
        CancellationToken cancellationToken = default);
}
