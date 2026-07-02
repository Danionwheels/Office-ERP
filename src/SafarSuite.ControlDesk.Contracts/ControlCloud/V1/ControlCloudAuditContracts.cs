namespace SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

public sealed record ControlCloudAuditEventResponse(
    Guid AuditEventId,
    Guid? ClientId,
    Guid? InvitationId,
    Guid? UserId,
    string SubjectEmail,
    string EventType,
    string Actor,
    string Detail,
    DateTimeOffset OccurredAtUtc);

public sealed record ControlCloudAuditEventsResponse(
    IReadOnlyList<ControlCloudAuditEventResponse> Events);
