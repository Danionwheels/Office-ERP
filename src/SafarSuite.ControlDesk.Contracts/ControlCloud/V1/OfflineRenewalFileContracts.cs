namespace SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

public static class ControlCloudOfflineRenewalFileFormat
{
    public const string Version = "safarsuite-offline-renewal-v1";
}

public sealed record ControlCloudOfflineRenewalFileResponse(
    string FormatVersion,
    Guid RenewalFileId,
    Guid ClientId,
    string InstallationId,
    DateTimeOffset GeneratedAtUtc,
    string GeneratedBy,
    string Reason,
    ClientPortalSignedEntitlementBundleResponse SignedBundle);
