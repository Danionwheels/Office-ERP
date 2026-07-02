using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Application.Modules.Audit.ListControlCloudAuditEvents;

public sealed record ListControlCloudAuditEventsResult(
    IReadOnlyList<ClientPortalAuditRecord> Events);
