namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListCloudInstallationAuditEvents;

public sealed record ListCloudInstallationAuditEventsQuery(
    Guid ClientId,
    string InstallationId,
    int Take);
