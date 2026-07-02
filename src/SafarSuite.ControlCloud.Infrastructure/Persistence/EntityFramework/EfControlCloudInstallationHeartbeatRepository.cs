using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;

public sealed class EfControlCloudInstallationHeartbeatRepository
    : IControlCloudInstallationHeartbeatRepository
{
    private readonly ControlCloudDbContext _dbContext;

    public EfControlCloudInstallationHeartbeatRepository(
        ControlCloudDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        ControlCloudInstallationHeartbeat heartbeat,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.InstallationHeartbeats.AddAsync(
            FromDomain(heartbeat),
            cancellationToken);
    }

    public async Task<ControlCloudInstallationHeartbeat?> GetLatestByInstallationIdAsync(
        string installationId,
        CancellationToken cancellationToken = default)
    {
        var cleanInstallationId = installationId.Trim();
        var entity = await _dbContext.InstallationHeartbeats
            .Where(heartbeat => heartbeat.InstallationId == cleanInstallationId)
            .OrderBy(heartbeat => heartbeat.ReceivedAtUtc)
            .ThenBy(heartbeat => heartbeat.HeartbeatId)
            .LastOrDefaultAsync(cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    private static ControlCloudInstallationHeartbeat ToDomain(
        ControlCloudInstallationHeartbeatEntity entity)
    {
        return new ControlCloudInstallationHeartbeat(
            entity.HeartbeatId,
            entity.ClientId,
            entity.InstallationId,
            entity.HeartbeatStatus,
            entity.ReceivedAtUtc,
            entity.ReportedAtUtc,
            entity.LicenseStatus,
            entity.EntitlementVersion,
            entity.PaidUntil,
            entity.WarningStartsAt,
            entity.GraceUntil,
            entity.OfflineValidUntil,
            entity.LocalServerVersion,
            entity.Detail);
    }

    private static ControlCloudInstallationHeartbeatEntity FromDomain(
        ControlCloudInstallationHeartbeat heartbeat)
    {
        return new ControlCloudInstallationHeartbeatEntity
        {
            HeartbeatId = heartbeat.HeartbeatId,
            ClientId = heartbeat.ClientId,
            InstallationId = heartbeat.InstallationId,
            HeartbeatStatus = heartbeat.HeartbeatStatus,
            ReceivedAtUtc = heartbeat.ReceivedAtUtc,
            ReportedAtUtc = heartbeat.ReportedAtUtc,
            LicenseStatus = heartbeat.LicenseStatus,
            EntitlementVersion = heartbeat.EntitlementVersion,
            PaidUntil = heartbeat.PaidUntil,
            WarningStartsAt = heartbeat.WarningStartsAt,
            GraceUntil = heartbeat.GraceUntil,
            OfflineValidUntil = heartbeat.OfflineValidUntil,
            LocalServerVersion = heartbeat.LocalServerVersion,
            Detail = heartbeat.Detail
        };
    }
}
