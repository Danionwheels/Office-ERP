namespace SafarSuite.ControlCloud.Application.Modules.Audit.ListControlCloudAuditEvents;

public sealed record ListControlCloudAuditEventsQuery(
    Guid? ClientId,
    string? EventType,
    int Take);
