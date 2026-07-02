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
    DateTimeOffset? LastAttemptedAtUtc,
    DateTimeOffset? NextAttemptAtUtc,
    DateTimeOffset? SentAtUtc,
    DateTimeOffset? FailedAtUtc,
    string? FailureReason);

public sealed record PublishCloudOutboxMessagesResponse(
    int RequestedBatchSize,
    int PublishedCount,
    int FailedCount,
    IReadOnlyCollection<PublishedCloudOutboxMessageResponse> Messages);

public sealed record PublishedCloudOutboxMessageResponse(
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

public sealed record QueueCloudInstallationSupportCommandRequest(
    string CommandType,
    string Reason,
    string RequestedBy,
    int ExpiresInHours);

public sealed record QueueCloudInstallationSupportCommandResponse(
    Guid CommandId,
    Guid ClientId,
    string InstallationId,
    long CommandVersion,
    string CommandType,
    string Status,
    string IdempotencyKey,
    DateTimeOffset QueuedAtUtc,
    DateTimeOffset? NotBeforeUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? AcknowledgedAtUtc,
    string? AcknowledgementStatus,
    string? AcknowledgementDetail,
    string SignatureKeyId,
    string PayloadSha256);
