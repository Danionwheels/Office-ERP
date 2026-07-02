using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.LocalServer.Application.Diagnostics.UploadDiagnosticsToControlCloud;

public sealed record UploadDiagnosticsToControlCloudCommand(
    LocalServerDiagnosticBundleResponse Bundle,
    string UploadedBy,
    string Reason);
