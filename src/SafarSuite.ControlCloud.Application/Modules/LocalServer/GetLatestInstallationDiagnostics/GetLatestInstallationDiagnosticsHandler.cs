using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.GetLatestInstallationDiagnostics;

public sealed class GetLatestInstallationDiagnosticsHandler
{
    private readonly IControlCloudClientInstallationRepository _installations;
    private readonly IControlCloudInstallationDiagnosticReportRepository _reports;

    public GetLatestInstallationDiagnosticsHandler(
        IControlCloudClientInstallationRepository installations,
        IControlCloudInstallationDiagnosticReportRepository reports)
    {
        _installations = installations;
        _reports = reports;
    }

    public async Task<GetLatestInstallationDiagnosticsResult> HandleAsync(
        GetLatestInstallationDiagnosticsQuery query,
        CancellationToken cancellationToken = default)
    {
        var installationId = query.InstallationId.Trim();

        if (query.ClientId == Guid.Empty)
        {
            return GetLatestInstallationDiagnosticsResult.Failure(
                "ClientIdRequired",
                "Client id is required before reading diagnostics.");
        }

        if (installationId.Length == 0)
        {
            return GetLatestInstallationDiagnosticsResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before reading diagnostics.");
        }

        var installation = await _installations.GetByInstallationIdAsync(
            installationId,
            cancellationToken);

        if (installation is null)
        {
            return GetLatestInstallationDiagnosticsResult.Failure(
                "InstallationNotFound",
                "Installation is not registered.");
        }

        if (installation.ClientId != query.ClientId)
        {
            return GetLatestInstallationDiagnosticsResult.Failure(
                "InstallationClientMismatch",
                "Installation id is already bound to another client.");
        }

        var report = await _reports.GetLatestByInstallationIdAsync(
            installationId,
            cancellationToken);

        return report is null
            ? GetLatestInstallationDiagnosticsResult.Failure(
                "DiagnosticsNotFound",
                "No diagnostics report has been uploaded for this installation.")
            : GetLatestInstallationDiagnosticsResult.Success(report);
    }
}
