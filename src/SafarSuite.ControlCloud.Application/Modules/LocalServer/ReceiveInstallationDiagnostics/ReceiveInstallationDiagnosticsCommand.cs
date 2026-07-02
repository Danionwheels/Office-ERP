using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.ReceiveInstallationDiagnostics;

public sealed record ReceiveInstallationDiagnosticsCommand(
    Guid ClientId,
    string InstallationId,
    string UploadedBy,
    string Reason,
    LocalServerDiagnosticBundleResponse Bundle);
