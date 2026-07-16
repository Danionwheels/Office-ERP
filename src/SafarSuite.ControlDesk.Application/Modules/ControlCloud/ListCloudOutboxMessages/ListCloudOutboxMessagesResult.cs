namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListCloudOutboxMessages;

public sealed record ListCloudOutboxMessagesResult(
    IReadOnlyCollection<CloudOutboxMessageSummaryResult> Messages,
    int PageSize,
    bool HasMore,
    string? NextCursor,
    CloudOutboxMessageRegisterSummaryResult Summary);

public sealed record CloudOutboxMessageSummaryResult(
    Guid CloudOutboxMessageId,
    Guid? ClientId,
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

public sealed record CloudOutboxMessageRegisterSummaryResult(
    long TotalCount,
    long PendingCount,
    long FailedCount,
    long SentCount,
    long ReadyForPublishingCount,
    long TotalAttemptCount);
