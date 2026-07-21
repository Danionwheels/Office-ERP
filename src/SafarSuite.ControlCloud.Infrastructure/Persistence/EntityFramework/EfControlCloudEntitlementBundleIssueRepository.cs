using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;

public sealed class EfControlCloudEntitlementBundleIssueRepository
    : IControlCloudEntitlementBundleIssueRepository
{
    private readonly ControlCloudDbContext _dbContext;

    public EfControlCloudEntitlementBundleIssueRepository(ControlCloudDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        ControlCloudEntitlementBundleIssue issue,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.EntitlementBundleIssues.AddAsync(
            new ControlCloudEntitlementBundleIssueEntity
            {
                BundleIssueId = issue.BundleIssueId,
                ClientId = issue.ClientId,
                InstallationId = issue.InstallationId,
                EntitlementVersion = issue.EntitlementVersion,
                EntitlementSnapshotId = issue.EntitlementSnapshotId,
                ClientAccessRevisionId = issue.ClientAccessRevisionId,
                ContractRevisionNumber = issue.ContractRevisionNumber,
                ProductCatalogRevisionId = issue.ProductCatalogRevisionId,
                ProductCatalogRevisionNumber = issue.ProductCatalogRevisionNumber,
                IssuedAtUtc = issue.IssuedAtUtc,
                Algorithm = issue.Algorithm,
                KeyId = issue.KeyId,
                PayloadSha256 = issue.PayloadSha256,
                SignatureValue = issue.SignatureValue,
                PayloadJson = issue.PayloadJson,
                PaidUntil = issue.PaidUntil,
                WarningStartsAt = issue.WarningStartsAt,
                GraceUntil = issue.GraceUntil,
                OfflineValidUntil = issue.OfflineValidUntil,
                AllowedNamedUsers = issue.AllowedNamedUsers,
                AllowedConcurrentUsers = issue.AllowedConcurrentUsers,
                FeatureLimitCount = issue.FeatureLimitCount
            },
            cancellationToken);
    }

    public async Task<ControlCloudEntitlementBundleIssue?> GetLatestByInstallationIdAsync(
        string installationId,
        CancellationToken cancellationToken = default)
    {
        var cleanInstallationId = installationId.Trim();
        var entity = await _dbContext.EntitlementBundleIssues
            .Where(issue => issue.InstallationId == cleanInstallationId)
            .OrderBy(issue => issue.IssuedAtUtc)
            .ThenBy(issue => issue.BundleIssueId)
            .LastOrDefaultAsync(cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    private static ControlCloudEntitlementBundleIssue ToDomain(
        ControlCloudEntitlementBundleIssueEntity entity)
    {
        return new ControlCloudEntitlementBundleIssue(
            entity.BundleIssueId,
            entity.ClientId,
            entity.InstallationId,
            entity.EntitlementVersion,
            entity.EntitlementSnapshotId,
            entity.ClientAccessRevisionId,
            entity.ContractRevisionNumber,
            entity.ProductCatalogRevisionId,
            entity.ProductCatalogRevisionNumber,
            entity.IssuedAtUtc,
            entity.Algorithm,
            entity.KeyId,
            entity.PayloadSha256,
            entity.SignatureValue,
            entity.PayloadJson,
            entity.PaidUntil,
            entity.WarningStartsAt,
            entity.GraceUntil,
            entity.OfflineValidUntil,
            entity.AllowedNamedUsers,
            entity.AllowedConcurrentUsers,
            entity.FeatureLimitCount);
    }
}
