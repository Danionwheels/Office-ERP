namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.ControlCloud;

public sealed record ListCloudOutboxMessagesResponse(
    IReadOnlyCollection<CloudOutboxMessageResponse> Messages);

public sealed record CloudOutboxMessageResponse(
    Guid CloudOutboxMessageId,
    string MessageType,
    string SubjectType,
    string SubjectId,
    string PayloadJson,
    string Status,
    int AttemptCount,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset? SentAtUtc,
    DateTimeOffset? FailedAtUtc,
    string? FailureReason);
