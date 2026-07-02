namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.GetCloudInstallationDiagnostics;

public sealed record GetCloudInstallationDiagnosticsQuery(
    Guid ClientId,
    string InstallationId);
