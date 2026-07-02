namespace SafarSuite.LocalServer.Domain.Entitlements;

public sealed record LocalServerEntitlementImportAuditRecord(
    Guid AuditRecordId,
    string InstallationId,
    Guid? ClientId,
    string ImportSource,
    string ResultStatus,
    long? EntitlementVersion,
    Guid? BundleIssueId,
    string? FailureCode,
    string? Detail,
    string? PayloadSha256,
    string? SignatureKeyId,
    DateTimeOffset OccurredAtUtc);

public static class LocalServerEntitlementImportSources
{
    public const string DirectBundle = "DirectBundle";
    public const string ControlCloudPull = "ControlCloudPull";
    public const string OfflineRenewalFile = "OfflineRenewalFile";
}

public static class LocalServerEntitlementImportResultStatuses
{
    public const string Accepted = "Accepted";
    public const string Rejected = "Rejected";
}
