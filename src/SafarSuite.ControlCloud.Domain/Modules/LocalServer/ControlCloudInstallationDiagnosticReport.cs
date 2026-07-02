namespace SafarSuite.ControlCloud.Domain.Modules.LocalServer;

public sealed record ControlCloudInstallationDiagnosticReport(
    Guid DiagnosticReportId,
    Guid ClientId,
    string InstallationId,
    string Status,
    DateTimeOffset ReceivedAtUtc,
    DateTimeOffset GeneratedAtUtc,
    string UploadedBy,
    string Reason,
    string LocalServerVersion,
    string LicenseStatus,
    string BundleJson);

public static class ControlCloudInstallationDiagnosticReportStatuses
{
    public const string Received = "Received";
}
