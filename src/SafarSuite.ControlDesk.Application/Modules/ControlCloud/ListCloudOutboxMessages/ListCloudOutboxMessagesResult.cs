namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListCloudOutboxMessages;

public sealed record ListCloudOutboxMessagesResult(
    IReadOnlyCollection<CloudOutboxMessageSummaryResult> Messages);

public sealed record CloudOutboxMessageSummaryResult(
    Guid CloudOutboxMessageId,
    string MessageType,
    string SubjectType,
    string SubjectId,
    string PayloadJson,
    string Status,
    int AttemptCount,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset? LastAttemptedAtUtc,
    DateTimeOffset? NextAttemptAtUtc,
    DateTimeOffset? SentAtUtc,
    DateTimeOffset? FailedAtUtc,
    string? FailureReason);
