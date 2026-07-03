using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.LocalServer.Application.Diagnostics.Ports;

public interface ILocalServerRuntimeDiagnosticsCollector
{
    Task<LocalServerRuntimeDiagnosticsSnapshot> CollectAsync(
        LocalServerRuntimeDiagnosticsContext context,
        CancellationToken cancellationToken = default);
}

public sealed record LocalServerRuntimeDiagnosticsContext(
    Guid ClientId,
    string InstallationId,
    string LocalServerVersion,
    string MachineName,
    string OperatingSystem,
    LocalServerDeploymentProfileResponse? DeploymentProfile);

public sealed record LocalServerRuntimeDiagnosticsSnapshot(
    LocalServerDiagnosticRuntimeResponse? Runtime,
    LocalServerDiagnosticBootstrapResponse? Bootstrap,
    IReadOnlyCollection<LocalServerDiagnosticServiceResponse>? Services,
    IReadOnlyCollection<LocalServerDiagnosticRecentErrorResponse>? RecentErrors)
{
    public static LocalServerRuntimeDiagnosticsSnapshot Empty { get; } = new(
        Runtime: null,
        Bootstrap: null,
        Services: null,
        RecentErrors: null);
}
