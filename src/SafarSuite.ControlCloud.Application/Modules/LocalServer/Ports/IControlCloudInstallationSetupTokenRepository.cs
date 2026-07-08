using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;

public interface IControlCloudInstallationSetupTokenRepository
{
    Task<ControlCloudInstallationSetupToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ControlCloudInstallationSetupToken>> ListBootstrapPackagesAsync(
        Guid clientId,
        string installationId,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ControlCloudInstallationSetupToken setupToken,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        ControlCloudInstallationSetupToken setupToken,
        CancellationToken cancellationToken = default);
}
