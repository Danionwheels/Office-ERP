using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.LocalServer.Application.Diagnostics.CreateLocalServerDiagnosticsBundle;

public sealed record CreateLocalServerDiagnosticsBundleCommand(
    Guid ClientId,
    string InstallationId,
    string LocalServerVersion,
    string GeneratedBy,
    string Reason,
    string MachineName,
    string OperatingSystem,
    DateOnly? AsOfDate,
    LocalServerDiagnosticRuntimeResponse? Runtime = null,
    LocalServerDiagnosticBootstrapResponse? Bootstrap = null,
    IReadOnlyCollection<LocalServerDiagnosticServiceResponse>? Services = null,
    IReadOnlyCollection<LocalServerDiagnosticRecentErrorResponse>? RecentErrors = null,
    IReadOnlyCollection<LocalServerDiagnosticImportAuditResponse>? ImportAudit = null,
    LocalServerDeploymentProfileResponse? DeploymentProfile = null);
