namespace SafarSuite.LocalServer.Domain.Entitlements;

public sealed record LocalServerEntitlementTrustState(
    string InstallationId,
    long LastAcceptedEntitlementVersion,
    Guid? LastAcceptedBundleIssueId,
    DateTimeOffset? LastAcceptedBundleIssuedAtUtc,
    DateTimeOffset? LastAcceptedAtUtc,
    DateTimeOffset? LastSuccessfulCloudTimeUtc,
    DateTimeOffset? LastLocalCheckAtUtc,
    bool ClockMovedBackwards,
    DateTimeOffset? ClockMovedBackwardsDetectedAtUtc,
    string? LastClockWarning,
    string? LastReplayWarning,
    DateTimeOffset? LastReplayWarningAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static LocalServerEntitlementTrustState Empty(
        string installationId,
        DateTimeOffset createdAtUtc)
    {
        return new LocalServerEntitlementTrustState(
            NormalizeInstallationId(installationId),
            LastAcceptedEntitlementVersion: 0,
            LastAcceptedBundleIssueId: null,
            LastAcceptedBundleIssuedAtUtc: null,
            LastAcceptedAtUtc: null,
            LastSuccessfulCloudTimeUtc: null,
            LastLocalCheckAtUtc: null,
            ClockMovedBackwards: false,
            ClockMovedBackwardsDetectedAtUtc: null,
            LastClockWarning: null,
            LastReplayWarning: null,
            LastReplayWarningAtUtc: null,
            createdAtUtc);
    }

    public LocalServerEntitlementTrustState RecordLocalCheck(
        DateTimeOffset checkedAtUtc,
        TimeSpan backwardTolerance)
    {
        var movedBackwards = LastLocalCheckAtUtc is not null
            && checkedAtUtc.Add(backwardTolerance) < LastLocalCheckAtUtc.Value;

        return this with
        {
            LastLocalCheckAtUtc = checkedAtUtc,
            ClockMovedBackwards = ClockMovedBackwards || movedBackwards,
            ClockMovedBackwardsDetectedAtUtc = movedBackwards
                ? checkedAtUtc
                : ClockMovedBackwardsDetectedAtUtc,
            LastClockWarning = movedBackwards
                ? $"Local clock moved backwards. Last local check was {LastLocalCheckAtUtc:O}; current check is {checkedAtUtc:O}."
                : LastClockWarning,
            UpdatedAtUtc = checkedAtUtc
        };
    }

    public LocalServerEntitlementTrustState RecordAcceptedEntitlement(
        LocalServerCachedEntitlement entitlement,
        DateTimeOffset acceptedAtUtc)
    {
        return this with
        {
            InstallationId = NormalizeInstallationId(entitlement.InstallationId),
            LastAcceptedEntitlementVersion = entitlement.EntitlementVersion,
            LastAcceptedBundleIssueId = entitlement.BundleIssueId,
            LastAcceptedBundleIssuedAtUtc = entitlement.BundleIssuedAtUtc,
            LastAcceptedAtUtc = acceptedAtUtc,
            UpdatedAtUtc = acceptedAtUtc
        };
    }

    public LocalServerEntitlementTrustState RecordReplayWarning(
        string warning,
        DateTimeOffset detectedAtUtc)
    {
        return this with
        {
            LastReplayWarning = warning,
            LastReplayWarningAtUtc = detectedAtUtc,
            UpdatedAtUtc = detectedAtUtc
        };
    }

    public LocalServerEntitlementTrustState RecordSuccessfulCloudTime(
        DateTimeOffset cloudTimeUtc,
        DateTimeOffset checkedAtUtc)
    {
        var trustedCloudTime = LastSuccessfulCloudTimeUtc is null || cloudTimeUtc > LastSuccessfulCloudTimeUtc
            ? cloudTimeUtc
            : LastSuccessfulCloudTimeUtc.Value;

        return this with
        {
            LastSuccessfulCloudTimeUtc = trustedCloudTime,
            LastLocalCheckAtUtc = checkedAtUtc,
            UpdatedAtUtc = checkedAtUtc
        };
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
