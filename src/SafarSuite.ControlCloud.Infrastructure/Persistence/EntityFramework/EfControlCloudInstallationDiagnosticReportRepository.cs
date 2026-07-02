using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;

public sealed class EfControlCloudInstallationDiagnosticReportRepository
    : IControlCloudInstallationDiagnosticReportRepository
{
    private readonly ControlCloudDbContext _dbContext;

    public EfControlCloudInstallationDiagnosticReportRepository(
        ControlCloudDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        ControlCloudInstallationDiagnosticReport report,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.InstallationDiagnosticReports.AddAsync(
            FromDomain(report),
            cancellationToken);
    }

    public async Task<ControlCloudInstallationDiagnosticReport?> GetLatestByInstallationIdAsync(
        string installationId,
        CancellationToken cancellationToken = default)
    {
        var cleanInstallationId = installationId.Trim();
        var entity = await _dbContext.InstallationDiagnosticReports
            .Where(report => report.InstallationId == cleanInstallationId)
            .OrderBy(report => report.ReceivedAtUtc)
            .ThenBy(report => report.DiagnosticReportId)
            .LastOrDefaultAsync(cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    private static ControlCloudInstallationDiagnosticReport ToDomain(
        ControlCloudInstallationDiagnosticReportEntity entity)
    {
        return new ControlCloudInstallationDiagnosticReport(
            entity.DiagnosticReportId,
            entity.ClientId,
            entity.InstallationId,
            entity.Status,
            entity.ReceivedAtUtc,
            entity.GeneratedAtUtc,
            entity.UploadedBy,
            entity.Reason,
            entity.LocalServerVersion,
            entity.LicenseStatus,
            entity.BundleJson);
    }

    private static ControlCloudInstallationDiagnosticReportEntity FromDomain(
        ControlCloudInstallationDiagnosticReport report)
    {
        return new ControlCloudInstallationDiagnosticReportEntity
        {
            DiagnosticReportId = report.DiagnosticReportId,
            ClientId = report.ClientId,
            InstallationId = report.InstallationId,
            Status = report.Status,
            ReceivedAtUtc = report.ReceivedAtUtc,
            GeneratedAtUtc = report.GeneratedAtUtc,
            UploadedBy = report.UploadedBy,
            Reason = report.Reason,
            LocalServerVersion = report.LocalServerVersion,
            LicenseStatus = report.LicenseStatus,
            BundleJson = report.BundleJson
        };
    }
}
