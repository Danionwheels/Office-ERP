using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;

public sealed class EfControlCloudInstallationSetupTokenRepository
    : IControlCloudInstallationSetupTokenRepository
{
    private readonly ControlCloudDbContext _dbContext;

    public EfControlCloudInstallationSetupTokenRepository(ControlCloudDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ControlCloudInstallationSetupToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.InstallationSetupTokens
            .SingleOrDefaultAsync(
                setupToken => setupToken.TokenHash == tokenHash.Trim(),
                cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task AddAsync(
        ControlCloudInstallationSetupToken setupToken,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.InstallationSetupTokens.AddAsync(
            FromDomain(setupToken),
            cancellationToken);
    }

    public async Task SaveAsync(
        ControlCloudInstallationSetupToken setupToken,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.InstallationSetupTokens
            .SingleOrDefaultAsync(
                stored => stored.SetupTokenId == setupToken.SetupTokenId,
                cancellationToken);

        if (entity is null)
        {
            await AddAsync(setupToken, cancellationToken);

            return;
        }

        entity.ClientId = setupToken.ClientId;
        entity.InstallationId = setupToken.InstallationId;
        entity.TokenHash = setupToken.TokenHash;
        entity.Status = setupToken.Status;
        entity.CreatedBy = setupToken.CreatedBy;
        entity.DeploymentMode = setupToken.DeploymentMode;
        entity.ClientDeploymentMode = setupToken.DeploymentProfile.ClientDeploymentMode;
        entity.SiteId = setupToken.DeploymentProfile.SiteId;
        entity.SiteRole = setupToken.DeploymentProfile.SiteRole;
        entity.ParentSiteId = setupToken.DeploymentProfile.ParentSiteId;
        entity.BranchCode = setupToken.DeploymentProfile.BranchCode;
        entity.SyncTopologyId = setupToken.DeploymentProfile.SyncTopologyId;
        entity.CreatedAtUtc = setupToken.CreatedAtUtc;
        entity.ExpiresAtUtc = setupToken.ExpiresAtUtc;
        entity.ConsumedAtUtc = setupToken.ConsumedAtUtc;
        entity.ConsumedLocalServerVersion = setupToken.ConsumedLocalServerVersion;
    }

    private static ControlCloudInstallationSetupToken ToDomain(
        ControlCloudInstallationSetupTokenEntity entity)
    {
        return ControlCloudInstallationSetupToken.Restore(
            entity.SetupTokenId,
            entity.ClientId,
            entity.InstallationId,
            entity.TokenHash,
            entity.Status,
            entity.CreatedBy,
            entity.DeploymentMode,
            entity.ClientDeploymentMode,
            entity.SiteId,
            entity.SiteRole,
            entity.ParentSiteId,
            entity.BranchCode,
            entity.SyncTopologyId,
            entity.CreatedAtUtc,
            entity.ExpiresAtUtc,
            entity.ConsumedAtUtc,
            entity.ConsumedLocalServerVersion);
    }

    private static ControlCloudInstallationSetupTokenEntity FromDomain(
        ControlCloudInstallationSetupToken setupToken)
    {
        return new ControlCloudInstallationSetupTokenEntity
        {
            SetupTokenId = setupToken.SetupTokenId,
            ClientId = setupToken.ClientId,
            InstallationId = setupToken.InstallationId,
            TokenHash = setupToken.TokenHash,
            Status = setupToken.Status,
            CreatedBy = setupToken.CreatedBy,
            DeploymentMode = setupToken.DeploymentMode,
            ClientDeploymentMode = setupToken.DeploymentProfile.ClientDeploymentMode,
            SiteId = setupToken.DeploymentProfile.SiteId,
            SiteRole = setupToken.DeploymentProfile.SiteRole,
            ParentSiteId = setupToken.DeploymentProfile.ParentSiteId,
            BranchCode = setupToken.DeploymentProfile.BranchCode,
            SyncTopologyId = setupToken.DeploymentProfile.SyncTopologyId,
            CreatedAtUtc = setupToken.CreatedAtUtc,
            ExpiresAtUtc = setupToken.ExpiresAtUtc,
            ConsumedAtUtc = setupToken.ConsumedAtUtc,
            ConsumedLocalServerVersion = setupToken.ConsumedLocalServerVersion
        };
    }
}
