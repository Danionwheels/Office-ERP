using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed class GetClientPortalSignedEntitlementBundleHandler
{
    private const string BundleVersion = "5";
    private const int DefaultWarningDaysBeforePaidUntil = 7;

    private readonly IControlCloudClientCommercialProjectionRepository _projections;
    private readonly IControlCloudClientInstallationRepository _installations;
    private readonly IControlCloudEntitlementBundleIssueRepository _bundleIssues;
    private readonly IControlCloudEntitlementBundleSigner _signer;
    private readonly IControlCloudUnitOfWork _unitOfWork;
    private readonly IControlCloudClock _clock;
    private readonly ControlCloudEntitlementBundleIdentity _identity;

    public GetClientPortalSignedEntitlementBundleHandler(
        IControlCloudClientCommercialProjectionRepository projections,
        IControlCloudClientInstallationRepository installations,
        IControlCloudEntitlementBundleIssueRepository bundleIssues,
        IControlCloudEntitlementBundleSigner signer,
        IControlCloudUnitOfWork unitOfWork,
        IControlCloudClock clock,
        ControlCloudEntitlementBundleIdentity identity)
    {
        _projections = projections;
        _installations = installations;
        _bundleIssues = bundleIssues;
        _signer = signer;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _identity = identity;
    }

    public async Task<GetClientPortalSignedEntitlementBundleResult> HandleAsync(
        GetClientPortalSignedEntitlementBundleQuery query,
        CancellationToken cancellationToken = default)
    {
        var installationId = NormalizeInstallationId(query.InstallationId);

        if (installationId is null)
        {
            return GetClientPortalSignedEntitlementBundleResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before issuing an installation-bound entitlement bundle.");
        }

        var projection = await _projections.GetByClientIdAsync(query.ClientId, cancellationToken);
        var entitlement = projection?.LatestEntitlement;

        if (projection is null || entitlement is null)
        {
            return GetClientPortalSignedEntitlementBundleResult.Failure(
                "EntitlementNotFound",
                "No projected entitlement snapshot was found for this client.");
        }

        var bundleIssuedAtUtc = _clock.UtcNow;
        var effectiveFromUtc = (entitlement.EffectiveFromUtc ?? entitlement.IssuedAtUtc).ToUniversalTime();

        if (effectiveFromUtc > bundleIssuedAtUtc)
        {
            return GetClientPortalSignedEntitlementBundleResult.Failure(
                "EntitlementScheduled",
                $"Entitlement version {entitlement.EntitlementVersion} is scheduled for {effectiveFromUtc:O} and cannot be signed yet.");
        }

        var validFrom = DateOnly.FromDateTime(effectiveFromUtc.UtcDateTime);
        var warningStartsAt = entitlement.PaidUntil.AddDays(-DefaultWarningDaysBeforePaidUntil);
        var entitlementVersion = entitlement.EntitlementVersion;
        var clientAccessRevisionId = entitlement.ClientAccessRevisionId == Guid.Empty
            ? entitlement.EntitlementSnapshotId
            : entitlement.ClientAccessRevisionId;
        var bundleIssueId = Guid.NewGuid();

        if (entitlementVersion <= 0)
        {
            return GetClientPortalSignedEntitlementBundleResult.Failure(
                "EntitlementVersionInvalid",
                "The projected entitlement does not contain a valid Office-issued version.");
        }

        if (warningStartsAt < validFrom)
        {
            warningStartsAt = validFrom;
        }

        var payload = new ControlCloudEntitlementBundlePayload(
            BundleVersion,
            _identity.Issuer,
            _identity.Audience,
            projection.ClientId,
            installationId,
            entitlementVersion,
            bundleIssueId,
            entitlement.EntitlementSnapshotId,
            clientAccessRevisionId,
            entitlement.ContractId,
            entitlement.ContractRevisionNumber,
            entitlement.ProductCatalogRevisionId,
            entitlement.ProductCatalogRevisionNumber,
            entitlement.SourceInvoiceId,
            entitlement.SourceInvoiceNumber,
            entitlement.Status,
            bundleIssuedAtUtc,
            entitlement.IssuedAtUtc,
            validFrom,
            entitlement.PaidUntil,
            warningStartsAt,
            entitlement.GraceUntil,
            entitlement.OfflineValidUntil,
            entitlement.AllowedDevices,
            entitlement.AllowedBranches,
            entitlement.Modules
                .OrderBy(module => module.ModuleCode, StringComparer.Ordinal)
                .Select(module => new ControlCloudEntitlementBundleModule(
                    module.ModuleCode,
                    module.IsEnabled ? "Active" : "Disabled",
                    module.IsEnabled))
                .ToArray(),
            entitlement.AllowedNamedUsers,
            entitlement.AllowedConcurrentUsers,
            (entitlement.FeatureLimits ?? [])
                .OrderBy(limit => limit.ModuleCode, StringComparer.Ordinal)
                .ThenBy(limit => limit.FeatureCode, StringComparer.Ordinal)
                .Select(limit => new ControlCloudEntitlementBundleFeatureLimit(
                    limit.ModuleCode,
                    limit.FeatureCode,
                    limit.LimitValue,
                    limit.Unit))
                .ToArray(),
            effectiveFromUtc);

        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var installation = await _installations.GetByInstallationIdAsync(installationId, token);

                    if (installation is null)
                    {
                        return GetClientPortalSignedEntitlementBundleResult.Failure(
                            "InstallationNotRegistered",
                            "Installation must be registered with a setup token before an entitlement bundle can be issued.");
                    }

                    if (installation.ClientId != projection.ClientId)
                    {
                        return GetClientPortalSignedEntitlementBundleResult.Failure(
                            "InstallationClientMismatch",
                            "Installation id is already bound to another client.");
                    }

                    if (entitlementVersion < installation.LatestEntitlementVersion)
                    {
                        return GetClientPortalSignedEntitlementBundleResult.Failure(
                            "EntitlementVersionRejected",
                            "The projected entitlement is older than the latest bundle issued for this installation.");
                    }

                    var signedBundle = _signer.Sign(payload);
                    var issue = new ControlCloudEntitlementBundleIssue(
                        bundleIssueId,
                        projection.ClientId,
                        installationId,
                        entitlementVersion,
                        entitlement.EntitlementSnapshotId,
                        clientAccessRevisionId,
                        entitlement.ContractRevisionNumber,
                        entitlement.ProductCatalogRevisionId,
                        entitlement.ProductCatalogRevisionNumber,
                        bundleIssuedAtUtc,
                        signedBundle.Signature.Algorithm,
                        signedBundle.Signature.KeyId,
                        signedBundle.Signature.PayloadSha256,
                        signedBundle.Signature.Value,
                        signedBundle.PayloadJson,
                        entitlement.PaidUntil,
                        warningStartsAt,
                        entitlement.GraceUntil,
                        entitlement.OfflineValidUntil,
                        entitlement.AllowedNamedUsers,
                        entitlement.AllowedConcurrentUsers,
                        entitlement.FeatureLimits?.Count ?? 0);

                    installation.RecordBundleIssued(entitlementVersion, bundleIssuedAtUtc);

                    await _installations.SaveAsync(installation, token);

                    await _bundleIssues.AddAsync(issue, token);

                    return GetClientPortalSignedEntitlementBundleResult.Success(signedBundle);
                },
                cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return GetClientPortalSignedEntitlementBundleResult.Failure(
                "InstallationIdInvalid",
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return GetClientPortalSignedEntitlementBundleResult.Failure(
                "EntitlementVersionRejected",
                exception.Message);
        }
    }

    private static string? NormalizeInstallationId(string? installationId)
    {
        var normalized = installationId?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}

public sealed record ControlCloudEntitlementBundleIdentity(
    string Issuer,
    string Audience);
