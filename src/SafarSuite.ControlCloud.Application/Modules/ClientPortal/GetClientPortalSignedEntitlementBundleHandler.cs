using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed class GetClientPortalSignedEntitlementBundleHandler
{
    private const string BundleVersion = "1";
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
        var validFrom = DateOnly.FromDateTime(entitlement.IssuedAtUtc.UtcDateTime);
        var warningStartsAt = entitlement.PaidUntil.AddDays(-DefaultWarningDaysBeforePaidUntil);
        var entitlementVersion = entitlement.IssuedAtUtc.UtcDateTime.Ticks;
        var bundleIssueId = Guid.NewGuid();

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
            entitlement.ContractId,
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
                .ToArray());

        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var installation = await _installations.GetByInstallationIdAsync(installationId, token);
                    var isNewInstallation = installation is null;

                    if (installation is null)
                    {
                        installation = ControlCloudClientInstallation.Register(
                            projection.ClientId,
                            installationId,
                            bundleIssuedAtUtc);
                    }
                    else if (installation.ClientId != projection.ClientId)
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
                        bundleIssuedAtUtc,
                        signedBundle.Signature.Algorithm,
                        signedBundle.Signature.KeyId,
                        signedBundle.Signature.PayloadSha256,
                        signedBundle.Signature.Value,
                        signedBundle.PayloadJson,
                        entitlement.PaidUntil,
                        warningStartsAt,
                        entitlement.GraceUntil,
                        entitlement.OfflineValidUntil);

                    installation.RecordBundleIssued(entitlementVersion, bundleIssuedAtUtc);

                    if (isNewInstallation)
                    {
                        await _installations.AddAsync(installation, token);
                    }
                    else
                    {
                        await _installations.SaveAsync(installation, token);
                    }

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
