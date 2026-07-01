namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.PublishPendingCloudOutboxMessages;

public sealed record PublishPendingCloudOutboxMessagesResult(
    int RequestedBatchSize,
    int PublishedCount,
    int FailedCount,
    IReadOnlyCollection<PublishedCloudOutboxMessageResult> Messages);

public sealed record PublishedCloudOutboxMessageResult(
    Guid CloudOutboxMessageId,
    string MessageType,
    string SubjectType,
    string SubjectId,
    string Status,
    int AttemptCount,
    DateTimeOffset? LastAttemptedAtUtc,
    DateTimeOffset? NextAttemptAtUtc,
    DateTimeOffset? SentAtUtc,
    DateTimeOffset? FailedAtUtc,
    string? FailureReason,
    string? CloudReference,
    string? EnvelopeSignature);
