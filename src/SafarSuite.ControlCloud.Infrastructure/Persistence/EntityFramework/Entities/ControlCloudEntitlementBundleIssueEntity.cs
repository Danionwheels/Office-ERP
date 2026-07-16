namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

public sealed class ControlCloudEntitlementBundleIssueEntity
{
    public Guid BundleIssueId { get; set; }

    public Guid ClientId { get; set; }

    public string InstallationId { get; set; } = "";

    public long EntitlementVersion { get; set; }

    public Guid EntitlementSnapshotId { get; set; }

    public Guid ClientAccessRevisionId { get; set; }

    public long ContractRevisionNumber { get; set; }

    public Guid ProductCatalogRevisionId { get; set; }

    public long ProductCatalogRevisionNumber { get; set; }

    public DateTimeOffset IssuedAtUtc { get; set; }

    public string Algorithm { get; set; } = "";

    public string KeyId { get; set; } = "";

    public string PayloadSha256 { get; set; } = "";

    public string SignatureValue { get; set; } = "";

    public string PayloadJson { get; set; } = "{}";

    public DateOnly PaidUntil { get; set; }

    public DateOnly WarningStartsAt { get; set; }

    public DateOnly GraceUntil { get; set; }

    public DateOnly OfflineValidUntil { get; set; }

    public int? AllowedNamedUsers { get; set; }

    public int? AllowedConcurrentUsers { get; set; }

    public int FeatureLimitCount { get; set; }
}
