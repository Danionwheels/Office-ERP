namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.GetLatestInstallationDiagnostics;

public sealed record GetLatestInstallationDiagnosticsQuery(
    Guid ClientId,
    string InstallationId);
