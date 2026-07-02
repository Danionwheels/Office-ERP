namespace SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

public sealed record ControlCloudSignedEntitlementBundle(
    string PayloadJson,
    ControlCloudEntitlementBundlePayload Payload,
    ControlCloudEntitlementBundleSignature Signature);

public sealed record ControlCloudEntitlementBundlePayload(
    string BundleVersion,
    string Issuer,
    string Audience,
    Guid ClientId,
    string InstallationId,
    long EntitlementVersion,
    Guid BundleIssueId,
    Guid EntitlementSnapshotId,
    Guid ContractId,
    Guid SourceInvoiceId,
    string SourceInvoiceNumber,
    string Status,
    DateTimeOffset BundleIssuedAtUtc,
    DateTimeOffset EntitlementIssuedAtUtc,
    DateOnly ValidFrom,
    DateOnly PaidUntil,
    DateOnly WarningStartsAt,
    DateOnly GraceUntil,
    DateOnly OfflineValidUntil,
    int AllowedDevices,
    int AllowedBranches,
    IReadOnlyCollection<ControlCloudEntitlementBundleModule> Modules);

public sealed record ControlCloudEntitlementBundleModule(
    string ModuleCode,
    string Status,
    bool IsEnabled);

public sealed record ControlCloudEntitlementBundleSignature(
    string Algorithm,
    string KeyId,
    string PayloadSha256,
    string Value);

public sealed class ControlCloudClientInstallation
{
    private ControlCloudClientInstallation(
        Guid clientId,
        string installationId,
        string status,
        DateTimeOffset registeredAtUtc,
        DateTimeOffset? lastBundleIssuedAtUtc,
        long latestEntitlementVersion)
    {
        ClientId = clientId;
        InstallationId = installationId;
        Status = status;
        RegisteredAtUtc = registeredAtUtc;
        LastBundleIssuedAtUtc = lastBundleIssuedAtUtc;
        LatestEntitlementVersion = latestEntitlementVersion;
    }

    public Guid ClientId { get; }

    public string InstallationId { get; }

    public string Status { get; private set; }

    public DateTimeOffset RegisteredAtUtc { get; }

    public DateTimeOffset? LastBundleIssuedAtUtc { get; private set; }

    public long LatestEntitlementVersion { get; private set; }

    public static ControlCloudClientInstallation Register(
        Guid clientId,
        string installationId,
        DateTimeOffset registeredAtUtc)
    {
        return new ControlCloudClientInstallation(
            clientId,
            NormalizeInstallationId(installationId),
            "Active",
            registeredAtUtc,
            lastBundleIssuedAtUtc: null,
            latestEntitlementVersion: 0);
    }

    public static ControlCloudClientInstallation Restore(
        Guid clientId,
        string installationId,
        string status,
        DateTimeOffset registeredAtUtc,
        DateTimeOffset? lastBundleIssuedAtUtc,
        long latestEntitlementVersion)
    {
        return new ControlCloudClientInstallation(
            clientId,
            NormalizeInstallationId(installationId),
            string.IsNullOrWhiteSpace(status) ? "Active" : status.Trim(),
            registeredAtUtc,
            lastBundleIssuedAtUtc,
            latestEntitlementVersion);
    }

    public void RecordBundleIssued(
        long entitlementVersion,
        DateTimeOffset issuedAtUtc)
    {
        if (entitlementVersion < LatestEntitlementVersion)
        {
            throw new InvalidOperationException(
                "Cannot issue an older entitlement bundle than the latest bundle issued for this installation.");
        }

        LatestEntitlementVersion = entitlementVersion;
        LastBundleIssuedAtUtc = issuedAtUtc;
    }

    private static string NormalizeInstallationId(string installationId)
    {
        var normalized = installationId.Trim();

        if (normalized.Length == 0)
        {
            throw new ArgumentException("Installation id is required.", nameof(installationId));
        }

        return normalized;
    }
}

public sealed record ControlCloudEntitlementBundleIssue(
    Guid BundleIssueId,
    Guid ClientId,
    string InstallationId,
    long EntitlementVersion,
    Guid EntitlementSnapshotId,
    DateTimeOffset IssuedAtUtc,
    string Algorithm,
    string KeyId,
    string PayloadSha256,
    string SignatureValue,
    string PayloadJson,
    DateOnly PaidUntil,
    DateOnly WarningStartsAt,
    DateOnly GraceUntil,
    DateOnly OfflineValidUntil);
