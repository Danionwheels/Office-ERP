using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.GetInstallationStatus;

public sealed class GetInstallationStatusHandler
{
    private readonly IControlCloudClientInstallationRepository _installations;
    private readonly IControlCloudInstallationHeartbeatRepository _heartbeats;
    private readonly IControlCloudEntitlementBundleIssueRepository _bundleIssues;
    private readonly IControlCloudInstallationCommandRepository _commands;
    private readonly IControlCloudClock _clock;

    public GetInstallationStatusHandler(
        IControlCloudClientInstallationRepository installations,
        IControlCloudInstallationHeartbeatRepository heartbeats,
        IControlCloudEntitlementBundleIssueRepository bundleIssues,
        IControlCloudInstallationCommandRepository commands,
        IControlCloudClock clock)
    {
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
        var pendingCommands = await _commands.ListPendingAsync(
            installationId,
            _clock.UtcNow,
            cancellationToken);
        var latestCommand = await _commands.GetLatestByInstallationIdAsync(
            installationId,
            cancellationToken);
        var response = new ControlCloudInstallationStatusResponse(
            installation.ClientId,
            installation.InstallationId,
            installation.Status,
            ToResponse(installation.DeploymentProfile),
            installation.RegisteredAtUtc,
            installation.LastBundleIssuedAtUtc,
            installation.LatestEntitlementVersion,
            latestHeartbeat is null ? null : ToResponse(latestHeartbeat, installation.DeploymentProfile),
            latestEntitlement is null ? null : ToResponse(latestEntitlement),
            ToCommandStatus(pendingCommands.Count, latestCommand));

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
            ToResponse(heartbeat.PairingStatus));
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
        ControlCloudEntitlementBundleIssue issue)
    {
        return new ControlCloudInstallationEntitlementStatusResponse(
            issue.BundleIssueId,
            issue.EntitlementVersion,
            issue.EntitlementSnapshotId,
            issue.IssuedAtUtc,
            issue.PaidUntil,
            issue.WarningStartsAt,
            issue.GraceUntil,
            issue.OfflineValidUntil,
            issue.KeyId,
            issue.PayloadSha256);
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

    private static string? NormalizeInstallationId(string installationId)
    {
        var normalized = installationId.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
