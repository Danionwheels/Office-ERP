namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

public sealed class ControlCloudInstallationDiagnosticReportEntity
{
    public Guid DiagnosticReportId { get; set; }

    public Guid ClientId { get; set; }

    public string InstallationId { get; set; } = "";

    public string Status { get; set; } = "Received";

    public DateTimeOffset ReceivedAtUtc { get; set; }

    public DateTimeOffset GeneratedAtUtc { get; set; }

    public string UploadedBy { get; set; } = "";

    public string Reason { get; set; } = "";

    public string LocalServerVersion { get; set; } = "";

    public string LicenseStatus { get; set; } = "";

    public string BundleJson { get; set; } = "{}";
}
