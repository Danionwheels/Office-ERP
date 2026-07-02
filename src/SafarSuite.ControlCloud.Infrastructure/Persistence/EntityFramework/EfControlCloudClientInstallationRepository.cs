using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;

public sealed class EfControlCloudClientInstallationRepository : IControlCloudClientInstallationRepository
{
    private readonly ControlCloudDbContext _dbContext;

    public EfControlCloudClientInstallationRepository(ControlCloudDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ControlCloudClientInstallation?> GetByInstallationIdAsync(
        string installationId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ClientInstallations
            .SingleOrDefaultAsync(
                installation => installation.InstallationId == installationId.Trim(),
                cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task AddAsync(
        ControlCloudClientInstallation installation,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.ClientInstallations.AddAsync(
            FromDomain(installation),
            cancellationToken);
    }

    public async Task SaveAsync(
        ControlCloudClientInstallation installation,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ClientInstallations
            .SingleOrDefaultAsync(
                stored => stored.InstallationId == installation.InstallationId,
                cancellationToken);

        if (entity is null)
        {
            await AddAsync(installation, cancellationToken);

            return;
        }

        entity.ClientId = installation.ClientId;
        entity.Status = installation.Status;
        entity.RegisteredAtUtc = installation.RegisteredAtUtc;
        entity.LastBundleIssuedAtUtc = installation.LastBundleIssuedAtUtc;
        entity.LatestEntitlementVersion = installation.LatestEntitlementVersion;
    }

    private static ControlCloudClientInstallation ToDomain(ControlCloudClientInstallationEntity entity)
    {
        return ControlCloudClientInstallation.Restore(
            entity.ClientId,
            entity.InstallationId,
            entity.Status,
            entity.RegisteredAtUtc,
            entity.LastBundleIssuedAtUtc,
            entity.LatestEntitlementVersion);
    }

    private static ControlCloudClientInstallationEntity FromDomain(ControlCloudClientInstallation installation)
    {
        return new ControlCloudClientInstallationEntity
        {
            ClientId = installation.ClientId,
            InstallationId = installation.InstallationId,
            Status = installation.Status,
            RegisteredAtUtc = installation.RegisteredAtUtc,
            LastBundleIssuedAtUtc = installation.LastBundleIssuedAtUtc,
            LatestEntitlementVersion = installation.LatestEntitlementVersion
        };
    }
}
