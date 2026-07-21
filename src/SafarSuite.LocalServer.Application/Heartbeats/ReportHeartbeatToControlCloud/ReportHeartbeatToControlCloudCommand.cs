using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.LocalServer.Application.Heartbeats.ReportHeartbeatToControlCloud;

public sealed record ReportHeartbeatToControlCloudCommand(
    Guid ClientId,
    string InstallationId,
    string LocalServerVersion,
    DateOnly? AsOfDate = null,
    string? Detail = null,
    LocalServerPairingStatusResponse? PairingStatus = null);
