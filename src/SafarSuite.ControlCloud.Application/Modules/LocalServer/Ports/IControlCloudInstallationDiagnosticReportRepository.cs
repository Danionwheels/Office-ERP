using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;

public interface IControlCloudInstallationDiagnosticReportRepository
{
    Task AddAsync(
        ControlCloudInstallationDiagnosticReport report,
        CancellationToken cancellationToken = default);

    Task<ControlCloudInstallationDiagnosticReport?> GetLatestByInstallationIdAsync(
        string installationId,
        CancellationToken cancellationToken = default);
}
