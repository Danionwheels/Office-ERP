using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.ReportInstallationHeartbeat;

public sealed class ReportInstallationHeartbeatHandler
{
    private static readonly HashSet<string> AllowedLicenseStatuses =
    [
        "Active",
        "Warning",
        "Grace",
        "Restricted",
        "Expired",
        "Missing",
        "NotYetValid",
        "InstallationMismatch",
        "StatusInactive",
        "Unknown"
    ];

    private readonly IControlCloudClientInstallationRepository _installations;
    private readonly IControlCloudInstallationHeartbeatRepository _heartbeats;
    private readonly IControlCloudUnitOfWork _unitOfWork;
    private readonly IControlCloudClock _clock;

    public ReportInstallationHeartbeatHandler(
        IControlCloudClientInstallationRepository installations,
        IControlCloudInstallationHeartbeatRepository heartbeats,
        IControlCloudUnitOfWork unitOfWork,
        IControlCloudClock clock)
    {
        _installations = installations;
        _heartbeats = heartbeats;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ReportInstallationHeartbeatResult> HandleAsync(
        ReportInstallationHeartbeatCommand command,
        CancellationToken cancellationToken = default)
    {
        var installationId = NormalizeRequiredText(command.InstallationId, 160);

        if (installationId is null)
        {
            return ReportInstallationHeartbeatResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before reporting heartbeat.");
        }

        if (command.ClientId == Guid.Empty)
        {
            return ReportInstallationHeartbeatResult.Failure(
                "ClientIdRequired",
                "Client id is required before reporting heartbeat.");
        }

        var licenseStatus = NormalizeRequiredText(command.LicenseStatus, 32);

        if (licenseStatus is null || !AllowedLicenseStatuses.Contains(licenseStatus))
        {
            return ReportInstallationHeartbeatResult.Failure(
                "LicenseStatusInvalid",
                "License status is not recognized for heartbeat reporting.");
        }

        if (command.EntitlementState is not null
            && command.EntitlementState.EntitlementVersion != command.EntitlementVersion)
        {
            return ReportInstallationHeartbeatResult.Failure(
                "EntitlementStateInvalid",
                "Observed entitlement values must describe the reported entitlement version.");
        }

        return await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var installation = await _installations.GetByInstallationIdAsync(
                    installationId,
                    token);

                if (installation is null)
                {
                    return ReportInstallationHeartbeatResult.Failure(
                        "InstallationNotFound",
                        "Installation is not registered.");
                }

                if (installation.ClientId != command.ClientId)
                {
                    return ReportInstallationHeartbeatResult.Failure(
                        "InstallationClientMismatch",
                        "Installation id is already bound to another client.");
                }

                var receivedAtUtc = _clock.UtcNow;
                var reportedAtUtc = command.ReportedAtUtc == default
                    ? receivedAtUtc
                    : command.ReportedAtUtc;
                var heartbeat = new ControlCloudInstallationHeartbeat(
                    Guid.NewGuid(),
                    installation.ClientId,
                    installation.InstallationId,
                    ControlCloudInstallationHeartbeatStatuses.Received,
                    receivedAtUtc,
                    reportedAtUtc,
                    licenseStatus,
                    command.EntitlementVersion,
                    command.PaidUntil,
                    command.WarningStartsAt,
                    command.GraceUntil,
                    command.OfflineValidUntil,
                    NormalizeOptionalText(command.LocalServerVersion, 80),
                    NormalizeOptionalText(command.Detail, 1000),
                    ToPairingStatus(command.PairingStatus),
                    ToEntitlementState(command.EntitlementState));

                await _heartbeats.AddAsync(heartbeat, token);

                return ReportInstallationHeartbeatResult.Success(
                    heartbeat,
                    installation.DeploymentProfile);
            },
            cancellationToken);
    }

    private static string? NormalizeRequiredText(string? value, int maxLength)
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

    private static ControlCloudInstallationPairingStatus? ToPairingStatus(
        LocalServerPairingStatusResponse? pairingStatus)
    {
        if (pairingStatus is null)
        {
            return null;
        }

        return new ControlCloudInstallationPairingStatus(
            NormalizeOptionalText(pairingStatus.PairingMode, 40)
                ?? LocalServerPairingModes.ManagerApproval,
            Math.Max(0, pairingStatus.TotalDeviceCount),
            Math.Max(0, pairingStatus.PendingDeviceCount),
            Math.Max(0, pairingStatus.ApprovedDeviceCount),
            Math.Max(0, pairingStatus.SuspendedDeviceCount),
            Math.Max(0, pairingStatus.RevokedDeviceCount),
            pairingStatus.FirstManagerDeviceApproved,
            pairingStatus.FirstManagerDeviceApprovedAtUtc,
            pairingStatus.LastDeviceUpdatedAtUtc);
    }

    private static ControlCloudObservedEntitlementState? ToEntitlementState(
        ControlCloudEntitlementStateValuesResponse? state)
    {
        if (state is null)
        {
            return null;
        }

        return new ControlCloudObservedEntitlementState(
            state.EntitlementVersion,
            state.EffectiveFromUtc.ToUniversalTime(),
            NormalizeOptionalText(state.Status, 32) ?? "Unknown",
            state.PaidUntil,
            state.WarningStartsAt,
            state.GraceUntil,
            state.OfflineValidUntil,
            state.AllowedDevices,
            state.AllowedBranches,
            state.AllowedNamedUsers,
            state.AllowedConcurrentUsers,
            state.Modules.Select(module => new ControlCloudObservedEntitlementModule(
                NormalizeOptionalText(module.ModuleCode, 64) ?? "UNKNOWN",
                module.IsEnabled)).ToArray(),
            state.FeatureLimits.Select(limit => new ControlCloudObservedEntitlementFeatureLimit(
                NormalizeOptionalText(limit.ModuleCode, 64) ?? "UNKNOWN",
                NormalizeOptionalText(limit.FeatureCode, 64) ?? "UNKNOWN",
                Math.Max(0, limit.LimitValue),
                NormalizeOptionalText(limit.Unit, 32) ?? "COUNT")).ToArray());
    }
}
