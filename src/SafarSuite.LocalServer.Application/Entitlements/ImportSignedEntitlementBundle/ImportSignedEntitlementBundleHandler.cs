using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Common;
using SafarSuite.LocalServer.Application.Entitlements.Ports;
using SafarSuite.LocalServer.Domain.Entitlements;

namespace SafarSuite.LocalServer.Application.Entitlements.ImportSignedEntitlementBundle;

public sealed class ImportSignedEntitlementBundleHandler
{
    private static readonly TimeSpan ClockBackwardTolerance = TimeSpan.FromMinutes(5);

    private readonly ILocalServerEntitlementBundleVerifier _verifier;
    private readonly ILocalServerEntitlementCache _cache;
    private readonly ILocalServerEntitlementTrustStateStore _trustStateStore;
    private readonly ILocalServerClock _clock;
    private readonly ILocalServerEntitlementImportAuditStore? _importAuditStore;

    public ImportSignedEntitlementBundleHandler(
        ILocalServerEntitlementBundleVerifier verifier,
        ILocalServerEntitlementCache cache,
        ILocalServerEntitlementTrustStateStore trustStateStore,
        ILocalServerClock clock,
        ILocalServerEntitlementImportAuditStore? importAuditStore = null)
    {
        _verifier = verifier;
        _cache = cache;
        _trustStateStore = trustStateStore;
        _clock = clock;
        _importAuditStore = importAuditStore;
    }

    public async Task<ImportSignedEntitlementBundleResult> HandleAsync(
        ImportSignedEntitlementBundleCommand command,
        CancellationToken cancellationToken = default)
    {
        var expectedInstallationId = command.ExpectedInstallationId.Trim();
        var importedAtUtc = _clock.UtcNow;

        if (expectedInstallationId.Length == 0)
        {
            await RecordRejectedImportAsync(
                command,
                expectedInstallationId,
                "InstallationIdRequired",
                "Expected installation id is required before importing an entitlement bundle.",
                importedAtUtc,
                cancellationToken);

            return ImportSignedEntitlementBundleResult.Failure(
                "InstallationIdRequired",
                "Expected installation id is required before importing an entitlement bundle.");
        }

        var trustState = await LoadTrustStateAsync(
            expectedInstallationId,
            importedAtUtc,
            cancellationToken);
        trustState = trustState.RecordLocalCheck(
            importedAtUtc,
            ClockBackwardTolerance);

        var verification = _verifier.Verify(
            command.Bundle,
            expectedInstallationId,
            importedAtUtc);

        if (!verification.IsValid)
        {
            await _trustStateStore.SaveAsync(trustState, cancellationToken);
            await RecordRejectedImportAsync(
                command,
                expectedInstallationId,
                verification.FailureCode ?? "EntitlementBundleInvalid",
                verification.Detail ?? "Entitlement bundle verification failed.",
                importedAtUtc,
                cancellationToken);

            return ImportSignedEntitlementBundleResult.Failure(
                verification.FailureCode ?? "EntitlementBundleInvalid",
                verification.Detail ?? "Entitlement bundle verification failed.");
        }

        var incomingEntitlement = verification.Entitlement!;
        var cachedEntitlement = await _cache.GetCurrentAsync(cancellationToken);

        if (trustState.LastAcceptedEntitlementVersion > incomingEntitlement.EntitlementVersion)
        {
            await _trustStateStore.SaveAsync(trustState, cancellationToken);
            await RecordRejectedImportAsync(
                command,
                expectedInstallationId,
                "EntitlementVersionRejected",
                "Incoming entitlement bundle is older than the latest entitlement version accepted by this local server.",
                importedAtUtc,
                cancellationToken,
                incomingEntitlement);

            return ImportSignedEntitlementBundleResult.Failure(
                "EntitlementVersionRejected",
                "Incoming entitlement bundle is older than the latest entitlement version accepted by this local server.");
        }

        if (IsReplayOfAcceptedBundle(trustState, incomingEntitlement))
        {
            await _trustStateStore.SaveAsync(trustState, cancellationToken);
            await RecordRejectedImportAsync(
                command,
                expectedInstallationId,
                "EntitlementReplayRejected",
                "Incoming entitlement bundle has already been accepted or is not newer than the last accepted bundle issue.",
                importedAtUtc,
                cancellationToken,
                incomingEntitlement);

            return ImportSignedEntitlementBundleResult.Failure(
                "EntitlementReplayRejected",
                "Incoming entitlement bundle has already been accepted or is not newer than the last accepted bundle issue.");
        }

        if (trustState.LastSuccessfulCloudTimeUtc is not null
            && incomingEntitlement.BundleIssuedAtUtc < trustState.LastSuccessfulCloudTimeUtc.Value)
        {
            trustState = trustState.RecordReplayWarning(
                $"Incoming entitlement bundle was issued at {incomingEntitlement.BundleIssuedAtUtc:O}, before the last trusted Control Cloud time {trustState.LastSuccessfulCloudTimeUtc:O}.",
                importedAtUtc);
        }

        if (cachedEntitlement is not null
            && cachedEntitlement.InstallationId == expectedInstallationId
            && incomingEntitlement.EntitlementVersion < cachedEntitlement.EntitlementVersion)
        {
            await _trustStateStore.SaveAsync(trustState, cancellationToken);
            await RecordRejectedImportAsync(
                command,
                expectedInstallationId,
                "EntitlementVersionRejected",
                "Incoming entitlement bundle is older than the cached entitlement.",
                importedAtUtc,
                cancellationToken,
                incomingEntitlement);

            return ImportSignedEntitlementBundleResult.Failure(
                "EntitlementVersionRejected",
                "Incoming entitlement bundle is older than the cached entitlement.");
        }

        await _cache.SaveAsync(incomingEntitlement, cancellationToken);
        trustState = trustState.RecordAcceptedEntitlement(
            incomingEntitlement,
            importedAtUtc);
        await _trustStateStore.SaveAsync(trustState, cancellationToken);
        await RecordAcceptedImportAsync(
            command,
            expectedInstallationId,
            incomingEntitlement,
            importedAtUtc,
            cancellationToken);

        return ImportSignedEntitlementBundleResult.Success(incomingEntitlement);
    }

    private async Task<LocalServerEntitlementTrustState> LoadTrustStateAsync(
        string installationId,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken)
    {
        return await _trustStateStore.GetAsync(installationId, cancellationToken)
            ?? LocalServerEntitlementTrustState.Empty(installationId, createdAtUtc);
    }

    private static bool IsReplayOfAcceptedBundle(
        LocalServerEntitlementTrustState trustState,
        LocalServerCachedEntitlement incomingEntitlement)
    {
        if (trustState.LastAcceptedEntitlementVersion != incomingEntitlement.EntitlementVersion)
        {
            return false;
        }

        if (trustState.LastAcceptedBundleIssueId == incomingEntitlement.BundleIssueId)
        {
            return true;
        }

        return trustState.LastAcceptedBundleIssuedAtUtc is not null
            && incomingEntitlement.BundleIssuedAtUtc <= trustState.LastAcceptedBundleIssuedAtUtc.Value;
    }

    private Task RecordAcceptedImportAsync(
        ImportSignedEntitlementBundleCommand command,
        string expectedInstallationId,
        LocalServerCachedEntitlement entitlement,
        DateTimeOffset importedAtUtc,
        CancellationToken cancellationToken)
    {
        return RecordImportAuditAsync(
            command,
            expectedInstallationId,
            LocalServerEntitlementImportResultStatuses.Accepted,
            failureCode: null,
            detail: null,
            importedAtUtc,
            cancellationToken,
            entitlement);
    }

    private Task RecordRejectedImportAsync(
        ImportSignedEntitlementBundleCommand command,
        string expectedInstallationId,
        string failureCode,
        string detail,
        DateTimeOffset importedAtUtc,
        CancellationToken cancellationToken,
        LocalServerCachedEntitlement? entitlement = null)
    {
        return RecordImportAuditAsync(
            command,
            expectedInstallationId,
            LocalServerEntitlementImportResultStatuses.Rejected,
            failureCode,
            detail,
            importedAtUtc,
            cancellationToken,
            entitlement);
    }

    private async Task RecordImportAuditAsync(
        ImportSignedEntitlementBundleCommand command,
        string expectedInstallationId,
        string resultStatus,
        string? failureCode,
        string? detail,
        DateTimeOffset importedAtUtc,
        CancellationToken cancellationToken,
        LocalServerCachedEntitlement? entitlement = null)
    {
        if (_importAuditStore is null)
        {
            return;
        }

        var auditInstallationId = ResolveAuditInstallationId(
            expectedInstallationId,
            entitlement,
            command.Bundle);

        if (auditInstallationId.Length == 0)
        {
            return;
        }

        var record = new LocalServerEntitlementImportAuditRecord(
            Guid.NewGuid(),
            auditInstallationId,
            entitlement?.ClientId ?? ToAuditClientId(command.Bundle),
            NormalizeImportSource(command.ImportSource),
            resultStatus,
            entitlement?.EntitlementVersion ?? ToAuditEntitlementVersion(command.Bundle),
            entitlement?.BundleIssueId ?? ToAuditBundleIssueId(command.Bundle),
            NormalizeOptionalText(failureCode, 120),
            NormalizeOptionalText(detail, 500),
            NormalizeOptionalText(command.Bundle.Signature.PayloadSha256, 128),
            NormalizeOptionalText(command.Bundle.Signature.KeyId, 120),
            importedAtUtc);

        try
        {
            await _importAuditStore.AppendAsync(record, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Import success must not depend on local support-audit storage availability.
        }
    }

    private static string ResolveAuditInstallationId(
        string expectedInstallationId,
        LocalServerCachedEntitlement? entitlement,
        ClientPortalSignedEntitlementBundleResponse bundle)
    {
        var cleanExpectedInstallationId = expectedInstallationId.Trim();

        if (cleanExpectedInstallationId.Length > 0)
        {
            return cleanExpectedInstallationId;
        }

        if (!string.IsNullOrWhiteSpace(entitlement?.InstallationId))
        {
            return entitlement.InstallationId.Trim();
        }

        return bundle.Payload.InstallationId?.Trim() ?? "";
    }

    private static Guid? ToAuditClientId(
        ClientPortalSignedEntitlementBundleResponse bundle)
    {
        return bundle.Payload.ClientId == Guid.Empty
            ? null
            : bundle.Payload.ClientId;
    }

    private static long? ToAuditEntitlementVersion(
        ClientPortalSignedEntitlementBundleResponse bundle)
    {
        return bundle.Payload.EntitlementVersion > 0
            ? bundle.Payload.EntitlementVersion
            : null;
    }

    private static Guid? ToAuditBundleIssueId(
        ClientPortalSignedEntitlementBundleResponse bundle)
    {
        return bundle.Payload.BundleIssueId == Guid.Empty
            ? null
            : bundle.Payload.BundleIssueId;
    }

    private static string NormalizeImportSource(string? value)
    {
        return NormalizeOptionalText(value, 80)
            ?? LocalServerEntitlementImportSources.DirectBundle;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }
}
