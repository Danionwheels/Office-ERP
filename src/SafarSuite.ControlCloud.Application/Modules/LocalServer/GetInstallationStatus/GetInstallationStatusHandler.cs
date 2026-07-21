using System.Globalization;
using System.Text.Json;
using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.GetInstallationStatus;

public sealed class GetInstallationStatusHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IControlCloudClientCommercialProjectionRepository _projections;
    private readonly IControlCloudClientInstallationRepository _installations;
    private readonly IControlCloudInstallationHeartbeatRepository _heartbeats;
    private readonly IControlCloudEntitlementBundleIssueRepository _bundleIssues;
    private readonly IControlCloudInstallationCommandRepository _commands;
    private readonly IControlCloudClock _clock;

    public GetInstallationStatusHandler(
        IControlCloudClientCommercialProjectionRepository projections,
        IControlCloudClientInstallationRepository installations,
        IControlCloudInstallationHeartbeatRepository heartbeats,
        IControlCloudEntitlementBundleIssueRepository bundleIssues,
        IControlCloudInstallationCommandRepository commands,
        IControlCloudClock clock)
    {
        _projections = projections;
        _installations = installations;
        _heartbeats = heartbeats;
        _bundleIssues = bundleIssues;
        _commands = commands;
        _clock = clock;
    }

    public async Task<GetInstallationStatusResult> HandleAsync(
        GetInstallationStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        var installationId = NormalizeInstallationId(query.InstallationId);

        if (installationId is null)
        {
            return GetInstallationStatusResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before reading installation status.");
        }

        if (query.ClientId == Guid.Empty)
        {
            return GetInstallationStatusResult.Failure(
                "ClientIdRequired",
                "Client id is required before reading installation status.");
        }

        var installation = await _installations.GetByInstallationIdAsync(
            installationId,
            cancellationToken);

        if (installation is null)
        {
            return GetInstallationStatusResult.Failure(
                "InstallationNotFound",
                "Installation is not registered.");
        }

        if (installation.ClientId != query.ClientId)
        {
            return GetInstallationStatusResult.Failure(
                "InstallationClientMismatch",
                "Installation id is already bound to another client.");
        }

        var latestHeartbeat = await _heartbeats.GetLatestByInstallationIdAsync(
            installationId,
            cancellationToken);
        var latestEntitlement = await _bundleIssues.GetLatestByInstallationIdAsync(
            installationId,
            cancellationToken);
        var commercialProjection = await _projections.GetByClientIdAsync(
            installation.ClientId,
            cancellationToken);
        var pendingCommands = await _commands.ListPendingAsync(
            installationId,
            _clock.UtcNow,
            cancellationToken);
        var latestCommand = await _commands.GetLatestByInstallationIdAsync(
            installationId,
            cancellationToken);
        var desiredState = ToDesiredState(commercialProjection?.LatestEntitlement);
        var deliveredState = ToDeliveredState(latestEntitlement);
        var observedState = ToObservedState(latestHeartbeat);
        var evaluatedAtUtc = _clock.UtcNow;
        var response = new ControlCloudInstallationStatusResponse(
            installation.ClientId,
            installation.InstallationId,
            installation.Status,
            ToResponse(installation.DeploymentProfile),
            installation.RegisteredAtUtc,
            installation.LastBundleIssuedAtUtc,
            installation.LatestEntitlementVersion,
            latestHeartbeat is null ? null : ToResponse(latestHeartbeat, installation.DeploymentProfile),
            latestEntitlement is null ? null : ToResponse(latestEntitlement, deliveredState),
            ToSyncStatus(
                commercialProjection?.LatestEntitlement?.EntitlementVersion,
                commercialProjection?.LatestEntitlement?.EffectiveFromUtc
                ?? commercialProjection?.LatestEntitlement?.IssuedAtUtc,
                latestEntitlement?.EntitlementVersion,
                latestHeartbeat?.EntitlementVersion,
                evaluatedAtUtc),
            ToCommandStatus(pendingCommands.Count, latestCommand),
            Reconcile(desiredState, deliveredState, observedState, evaluatedAtUtc));

        return GetInstallationStatusResult.Success(response);
    }

    private static LocalServerHeartbeatResponse ToResponse(
        ControlCloudInstallationHeartbeat heartbeat,
        ControlCloudInstallationDeploymentProfile deploymentProfile)
    {
        return new LocalServerHeartbeatResponse(
            heartbeat.HeartbeatId,
            heartbeat.InstallationId,
            heartbeat.ClientId,
            heartbeat.HeartbeatStatus,
            heartbeat.ReceivedAtUtc,
            heartbeat.ReportedAtUtc,
            heartbeat.LicenseStatus,
            heartbeat.EntitlementVersion,
            heartbeat.PaidUntil,
            heartbeat.WarningStartsAt,
            heartbeat.GraceUntil,
            heartbeat.OfflineValidUntil,
            heartbeat.LocalServerVersion,
            heartbeat.Detail,
            ToResponse(deploymentProfile),
            ToResponse(heartbeat.PairingStatus),
            ToResponse(heartbeat.EntitlementState));
    }

    private static LocalServerDeploymentProfileResponse ToResponse(
        ControlCloudInstallationDeploymentProfile deploymentProfile)
    {
        return new LocalServerDeploymentProfileResponse(
            deploymentProfile.BootstrapMode,
            deploymentProfile.ClientDeploymentMode,
            deploymentProfile.SiteId,
            deploymentProfile.SiteRole,
            deploymentProfile.ParentSiteId,
            deploymentProfile.BranchCode,
            deploymentProfile.SyncTopologyId);
    }

    private static LocalServerPairingStatusResponse? ToResponse(
        ControlCloudInstallationPairingStatus? pairingStatus)
    {
        return pairingStatus is null
            ? null
            : new LocalServerPairingStatusResponse(
                pairingStatus.PairingMode,
                pairingStatus.TotalDeviceCount,
                pairingStatus.PendingDeviceCount,
                pairingStatus.ApprovedDeviceCount,
                pairingStatus.SuspendedDeviceCount,
                pairingStatus.RevokedDeviceCount,
                pairingStatus.FirstManagerDeviceApproved,
                pairingStatus.FirstManagerDeviceApprovedAtUtc,
                pairingStatus.LastDeviceUpdatedAtUtc);
    }

    private static ControlCloudInstallationEntitlementStatusResponse ToResponse(
        ControlCloudEntitlementBundleIssue issue,
        ControlCloudEntitlementStateValuesResponse? deliveredState)
    {
        return new ControlCloudInstallationEntitlementStatusResponse(
            issue.BundleIssueId,
            issue.EntitlementVersion,
            issue.EntitlementSnapshotId,
            issue.ClientAccessRevisionId == Guid.Empty
                ? issue.EntitlementSnapshotId
                : issue.ClientAccessRevisionId,
            issue.ContractRevisionNumber,
            issue.ProductCatalogRevisionId,
            issue.ProductCatalogRevisionNumber,
            issue.IssuedAtUtc,
            issue.PaidUntil,
            issue.WarningStartsAt,
            issue.GraceUntil,
            issue.OfflineValidUntil,
            issue.KeyId,
            issue.PayloadSha256,
            issue.AllowedNamedUsers,
            issue.AllowedConcurrentUsers,
            issue.FeatureLimitCount,
            deliveredState?.EffectiveFromUtc);
    }

    private static ControlCloudInstallationCommandStatusResponse ToCommandStatus(
        int pendingCommandCount,
        ControlCloudInstallationCommand? latestCommand)
    {
        return new ControlCloudInstallationCommandStatusResponse(
            pendingCommandCount,
            latestCommand?.CommandVersion ?? 0,
            latestCommand?.CommandId,
            latestCommand?.CommandType,
            latestCommand?.Status,
            latestCommand?.QueuedAtUtc,
            latestCommand?.AcknowledgedAtUtc,
            latestCommand?.AcknowledgementStatus,
            latestCommand?.AcknowledgementDetail);
    }

    private static ControlCloudEntitlementSyncStatusResponse ToSyncStatus(
        long? desiredVersion,
        DateTimeOffset? effectiveFromUtc,
        long? signedVersion,
        long? observedVersion,
        DateTimeOffset evaluatedAtUtc)
    {
        if (!desiredVersion.HasValue || desiredVersion.Value <= 0)
        {
            return new ControlCloudEntitlementSyncStatusResponse(
                desiredVersion,
                signedVersion,
                observedVersion,
                "Unknown",
                "Control Cloud has not received an Office-issued entitlement version for this client.");
        }

        if (effectiveFromUtc.HasValue && effectiveFromUtc.Value > evaluatedAtUtc)
        {
            return new ControlCloudEntitlementSyncStatusResponse(
                desiredVersion,
                signedVersion,
                observedVersion,
                "Scheduled",
                $"Entitlement version {desiredVersion.Value} becomes eligible for signing at {effectiveFromUtc.Value.ToUniversalTime():O}.");
        }

        if (!signedVersion.HasValue || signedVersion.Value < desiredVersion.Value)
        {
            return new ControlCloudEntitlementSyncStatusResponse(
                desiredVersion,
                signedVersion,
                observedVersion,
                "SigningPending",
                $"Entitlement version {desiredVersion.Value} is projected but has not been signed for this installation.");
        }

        if (signedVersion.Value > desiredVersion.Value)
        {
            return new ControlCloudEntitlementSyncStatusResponse(
                desiredVersion,
                signedVersion,
                observedVersion,
                "Ahead",
                $"The latest signed version {signedVersion.Value} is newer than desired version {desiredVersion.Value}.");
        }

        if (!observedVersion.HasValue || observedVersion.Value < desiredVersion.Value)
        {
            return new ControlCloudEntitlementSyncStatusResponse(
                desiredVersion,
                signedVersion,
                observedVersion,
                "ApplyPending",
                observedVersion.HasValue
                    ? $"SafarSuite Server reports version {observedVersion.Value}; version {desiredVersion.Value} is ready to apply."
                    : $"Version {desiredVersion.Value} is signed, but SafarSuite Server has not reported an applied version.");
        }

        if (observedVersion.Value > desiredVersion.Value)
        {
            return new ControlCloudEntitlementSyncStatusResponse(
                desiredVersion,
                signedVersion,
                observedVersion,
                "Ahead",
                $"SafarSuite Server reports unknown newer version {observedVersion.Value}; desired version is {desiredVersion.Value}.");
        }

        return new ControlCloudEntitlementSyncStatusResponse(
            desiredVersion,
            signedVersion,
            observedVersion,
            "InSync",
            $"SafarSuite Server is enforcing desired entitlement version {desiredVersion.Value}.");
    }

    private static ControlCloudEntitlementStateValuesResponse? ToDesiredState(
        ControlCloudEntitlementProjection? entitlement)
    {
        return entitlement is null
            ? null
            : new ControlCloudEntitlementStateValuesResponse(
                entitlement.EntitlementVersion,
                (entitlement.EffectiveFromUtc ?? entitlement.IssuedAtUtc).ToUniversalTime(),
                entitlement.Status,
                entitlement.PaidUntil,
                WarningStartsAt: null,
                entitlement.GraceUntil,
                entitlement.OfflineValidUntil,
                entitlement.AllowedDevices,
                entitlement.AllowedBranches,
                entitlement.AllowedNamedUsers,
                entitlement.AllowedConcurrentUsers,
                entitlement.Modules.Select(module => new ControlCloudEntitlementStateModuleResponse(
                    module.ModuleCode,
                    module.IsEnabled)).ToArray(),
                (entitlement.FeatureLimits ?? []).Select(limit =>
                    new ControlCloudEntitlementStateFeatureLimitResponse(
                        limit.ModuleCode,
                        limit.FeatureCode,
                        limit.LimitValue,
                        limit.Unit)).ToArray());
    }

    private static ControlCloudEntitlementStateValuesResponse? ToDeliveredState(
        ControlCloudEntitlementBundleIssue? issue)
    {
        if (issue is null)
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<ControlCloudEntitlementBundlePayload>(
                issue.PayloadJson,
                JsonOptions);

            if (payload is not null)
            {
                return new ControlCloudEntitlementStateValuesResponse(
                    payload.EntitlementVersion,
                    (payload.EffectiveFromUtc
                     ?? new DateTimeOffset(payload.ValidFrom.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero))
                    .ToUniversalTime(),
                    payload.Status,
                    payload.PaidUntil,
                    payload.WarningStartsAt,
                    payload.GraceUntil,
                    payload.OfflineValidUntil,
                    payload.AllowedDevices,
                    payload.AllowedBranches,
                    payload.AllowedNamedUsers,
                    payload.AllowedConcurrentUsers,
                    payload.Modules.Select(module => new ControlCloudEntitlementStateModuleResponse(
                        module.ModuleCode,
                        module.IsEnabled)).ToArray(),
                    (payload.FeatureLimits ?? []).Select(limit =>
                        new ControlCloudEntitlementStateFeatureLimitResponse(
                            limit.ModuleCode,
                            limit.FeatureCode,
                            limit.LimitValue,
                            limit.Unit)).ToArray());
            }
        }
        catch (JsonException)
        {
        }

        return new ControlCloudEntitlementStateValuesResponse(
            issue.EntitlementVersion,
            issue.IssuedAtUtc.ToUniversalTime(),
            "Unknown",
            issue.PaidUntil,
            issue.WarningStartsAt,
            issue.GraceUntil,
            issue.OfflineValidUntil,
            AllowedDevices: null,
            AllowedBranches: null,
            issue.AllowedNamedUsers,
            issue.AllowedConcurrentUsers,
            [],
            []);
    }

    private static ControlCloudEntitlementStateValuesResponse? ToObservedState(
        ControlCloudInstallationHeartbeat? heartbeat)
    {
        return heartbeat?.EntitlementState is null
            ? null
            : ToResponse(heartbeat.EntitlementState);
    }

    private static ControlCloudEntitlementStateValuesResponse? ToResponse(
        ControlCloudObservedEntitlementState? state)
    {
        return state is null
            ? null
            : new ControlCloudEntitlementStateValuesResponse(
                state.EntitlementVersion,
                state.EffectiveFromUtc,
                state.Status,
                state.PaidUntil,
                state.WarningStartsAt,
                state.GraceUntil,
                state.OfflineValidUntil,
                state.AllowedDevices,
                state.AllowedBranches,
                state.AllowedNamedUsers,
                state.AllowedConcurrentUsers,
                state.Modules.Select(module => new ControlCloudEntitlementStateModuleResponse(
                    module.ModuleCode,
                    module.IsEnabled)).ToArray(),
                state.FeatureLimits.Select(limit => new ControlCloudEntitlementStateFeatureLimitResponse(
                    limit.ModuleCode,
                    limit.FeatureCode,
                    limit.LimitValue,
                    limit.Unit)).ToArray());
    }

    private static ControlCloudEntitlementReconciliationResponse Reconcile(
        ControlCloudEntitlementStateValuesResponse? desired,
        ControlCloudEntitlementStateValuesResponse? delivered,
        ControlCloudEntitlementStateValuesResponse? observed,
        DateTimeOffset evaluatedAtUtc)
    {
        if (desired is null)
        {
            return new ControlCloudEntitlementReconciliationResponse(
                evaluatedAtUtc,
                "Unknown",
                "Control Cloud has no Office desired-access state for this client.",
                desired,
                delivered,
                observed,
                []);
        }

        var isScheduled = desired.EffectiveFromUtc > evaluatedAtUtc;
        var desiredValues = ToComparableValues(desired);
        var deliveredValues = delivered is null ? [] : ToComparableValues(delivered);
        var observedValues = observed is null ? [] : ToComparableValues(observed);
        var differences = desiredValues
            .Select(pair => CreateDifference(
                pair.Key,
                pair.Value,
                deliveredValues.GetValueOrDefault(pair.Key),
                observedValues.GetValueOrDefault(pair.Key),
                isScheduled,
                desired.EntitlementVersion,
                delivered?.EntitlementVersion,
                observed?.EntitlementVersion))
            .Where(difference => difference is not null)
            .Cast<ControlCloudEntitlementDifferenceResponse>()
            .ToArray();

        if (isScheduled)
        {
            return new ControlCloudEntitlementReconciliationResponse(
                evaluatedAtUtc,
                "Scheduled",
                $"Desired entitlement version {desired.EntitlementVersion} becomes effective at {desired.EffectiveFromUtc:O}; current delivered and observed values may remain on the prior version until then.",
                desired,
                delivered,
                observed,
                differences);
        }

        if (delivered is null || delivered.EntitlementVersion < desired.EntitlementVersion)
        {
            return CreateReconciliation(
                "DeliveryPending",
                $"Desired version {desired.EntitlementVersion} has not been signed for this installation.",
                differences);
        }

        if (delivered.EntitlementVersion > desired.EntitlementVersion)
        {
            return CreateReconciliation(
                "Ahead",
                $"Delivered version {delivered.EntitlementVersion} is newer than desired version {desired.EntitlementVersion}.",
                differences);
        }

        if (differences.Any(difference => difference.State == "DeliveryDrift"))
        {
            return CreateReconciliation(
                "DeliveryDrift",
                "The signed payload differs from Office desired-access values.",
                differences);
        }

        if (observed is null || observed.EntitlementVersion < desired.EntitlementVersion)
        {
            return CreateReconciliation(
                "ApplyPending",
                $"Desired version {desired.EntitlementVersion} is delivered but has not been observed on SafarSuite Server.",
                differences);
        }

        if (observed.EntitlementVersion > desired.EntitlementVersion)
        {
            return CreateReconciliation(
                "Ahead",
                $"Observed version {observed.EntitlementVersion} is newer than desired version {desired.EntitlementVersion}.",
                differences);
        }

        if (differences.Length > 0)
        {
            return CreateReconciliation(
                "ObservedDrift",
                "SafarSuite Server reports values that differ from Office desired access.",
                differences);
        }

        return CreateReconciliation(
            "InSync",
            $"Desired, delivered, and observed values match for entitlement version {desired.EntitlementVersion}.",
            differences);

        ControlCloudEntitlementReconciliationResponse CreateReconciliation(
            string state,
            string detail,
            IReadOnlyCollection<ControlCloudEntitlementDifferenceResponse> items)
        {
            return new ControlCloudEntitlementReconciliationResponse(
                evaluatedAtUtc,
                state,
                detail,
                desired,
                delivered,
                observed,
                items);
        }
    }

    private static ControlCloudEntitlementDifferenceResponse? CreateDifference(
        string field,
        string desiredValue,
        string? deliveredValue,
        string? observedValue,
        bool isScheduled,
        long desiredVersion,
        long? deliveredVersion,
        long? observedVersion)
    {
        if (string.Equals(desiredValue, deliveredValue, StringComparison.Ordinal)
            && string.Equals(desiredValue, observedValue, StringComparison.Ordinal))
        {
            return null;
        }

        var state = isScheduled
            ? "Scheduled"
            : !string.Equals(desiredValue, deliveredValue, StringComparison.Ordinal)
                ? deliveredVersion.GetValueOrDefault() < desiredVersion ? "DeliveryPending" : "DeliveryDrift"
                : observedVersion.GetValueOrDefault() < desiredVersion ? "ApplyPending" : "ObservedDrift";

        return new ControlCloudEntitlementDifferenceResponse(
            field,
            desiredValue,
            deliveredValue,
            observedValue,
            state,
            state switch
            {
                "Scheduled" => "Difference is expected until the desired effective time.",
                "DeliveryPending" => "The installation has not received a signed payload for this desired value.",
                "DeliveryDrift" => "The signed payload does not match Office desired access.",
                "ApplyPending" => "The signed value has not yet been observed on SafarSuite Server.",
                _ => "SafarSuite Server reports a value different from Office desired access."
            });
    }

    private static Dictionary<string, string> ToComparableValues(
        ControlCloudEntitlementStateValuesResponse state)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["EntitlementVersion"] = state.EntitlementVersion.ToString(CultureInfo.InvariantCulture),
            ["EffectiveFromUtc"] = state.EffectiveFromUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            ["Status"] = state.Status.Trim(),
            ["PaidUntil"] = state.PaidUntil.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["GraceUntil"] = state.GraceUntil.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["OfflineValidUntil"] = state.OfflineValidUntil.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["AllowedDevices"] = FormatOptional(state.AllowedDevices),
            ["AllowedBranches"] = FormatOptional(state.AllowedBranches),
            ["AllowedNamedUsers"] = FormatOptional(state.AllowedNamedUsers),
            ["AllowedConcurrentUsers"] = FormatOptional(state.AllowedConcurrentUsers),
            ["Modules"] = string.Join(
                ", ",
                state.Modules
                    .OrderBy(module => module.ModuleCode, StringComparer.Ordinal)
                    .Select(module => $"{module.ModuleCode.Trim().ToUpperInvariant()}={(module.IsEnabled ? "enabled" : "disabled")}")),
            ["FeatureLimits"] = string.Join(
                ", ",
                state.FeatureLimits
                    .OrderBy(limit => limit.ModuleCode, StringComparer.Ordinal)
                    .ThenBy(limit => limit.FeatureCode, StringComparer.Ordinal)
                    .Select(limit => $"{limit.ModuleCode.Trim().ToUpperInvariant()}.{limit.FeatureCode.Trim().ToUpperInvariant()}={limit.LimitValue.ToString(CultureInfo.InvariantCulture)} {limit.Unit.Trim().ToUpperInvariant()}"))
        };
    }

    private static string FormatOptional(int? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture) ?? "Unspecified";
    }

    private static string? NormalizeInstallationId(string installationId)
    {
        var normalized = installationId.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
