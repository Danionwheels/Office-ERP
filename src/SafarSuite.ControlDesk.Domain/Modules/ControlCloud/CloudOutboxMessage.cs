using SafarSuite.ControlDesk.Domain.SharedKernel;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Domain.Modules.ControlCloud;

public sealed class CloudOutboxMessage : Entity<CloudOutboxMessageId>
{
    private CloudOutboxMessage()
    {
        MessageType = string.Empty;
        SubjectType = string.Empty;
        SubjectId = string.Empty;
        PayloadJson = string.Empty;
    }

    private CloudOutboxMessage(
        CloudOutboxMessageId id,
        ClientId? clientId,
        string messageType,
        string subjectType,
        string subjectId,
        string payloadJson,
        DateTimeOffset occurredAtUtc)
        : base(id)
    {
        ClientId = clientId;
        MessageType = messageType;
        SubjectType = subjectType;
        SubjectId = subjectId;
        PayloadJson = payloadJson;
        OccurredAtUtc = occurredAtUtc;
        Status = CloudOutboxMessageStatus.Pending;
    }

    public ClientId? ClientId { get; private set; }

    public string MessageType { get; private set; }

    public string SubjectType { get; private set; }

    public string SubjectId { get; private set; }

    public string PayloadJson { get; private set; }

    public CloudOutboxMessageStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public DateTimeOffset? LastAttemptedAtUtc { get; private set; }

    public DateTimeOffset? NextAttemptAtUtc { get; private set; }

    public DateTimeOffset? SentAtUtc { get; private set; }

    public DateTimeOffset? FailedAtUtc { get; private set; }

    public string? FailureReason { get; private set; }

    public static CloudOutboxMessage Create(
        CloudOutboxMessageId id,
        ClientId clientId,
        string messageType,
        string subjectType,
        string subjectId,
        string payloadJson,
        DateTimeOffset occurredAtUtc)
    {
        return new CloudOutboxMessage(
            id,
            clientId,
            CleanRequiredText(messageType, nameof(messageType)),
            CleanRequiredText(subjectType, nameof(subjectType)),
            CleanRequiredText(subjectId, nameof(subjectId)),
            CleanRequiredText(payloadJson, nameof(payloadJson)),
            occurredAtUtc);
    }

    public static CloudOutboxMessage CreateSystem(
        CloudOutboxMessageId id,
        string messageType,
        string subjectType,
        string subjectId,
        string payloadJson,
        DateTimeOffset occurredAtUtc)
    {
        return new CloudOutboxMessage(
            id,
            clientId: null,
            CleanRequiredText(messageType, nameof(messageType)),
            CleanRequiredText(subjectType, nameof(subjectType)),
            CleanRequiredText(subjectId, nameof(subjectId)),
            CleanRequiredText(payloadJson, nameof(payloadJson)),
            occurredAtUtc);
    }

    public void MarkSent(DateTimeOffset sentAtUtc)
    {
        RegisterPublishAttempt(sentAtUtc);
        Status = CloudOutboxMessageStatus.Sent;
        SentAtUtc = sentAtUtc;
        FailedAtUtc = null;
        NextAttemptAtUtc = null;
        FailureReason = null;
    }

    public void MarkFailed(string reason, DateTimeOffset failedAtUtc, DateTimeOffset? nextAttemptAtUtc)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Failure reason is required.", nameof(reason));
        }

        if (nextAttemptAtUtc.HasValue && nextAttemptAtUtc.Value <= failedAtUtc)
        {
            throw new ArgumentException(
                "Next attempt time must be after the failure time.",
                nameof(nextAttemptAtUtc));
        }

        RegisterPublishAttempt(failedAtUtc);
        Status = CloudOutboxMessageStatus.Failed;
        FailedAtUtc = failedAtUtc;
        NextAttemptAtUtc = nextAttemptAtUtc;
        FailureReason = reason.Trim();
    }

    public bool IsReadyForPublishing(DateTimeOffset readyAtUtc, int maximumAttemptCount)
    {
        if (maximumAttemptCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAttemptCount));
        }

        if (Status == CloudOutboxMessageStatus.Pending)
        {
            return AttemptCount < maximumAttemptCount;
        }

        return Status == CloudOutboxMessageStatus.Failed
            && AttemptCount < maximumAttemptCount
            && NextAttemptAtUtc.HasValue
            && NextAttemptAtUtc.Value <= readyAtUtc;
    }

    private void RegisterPublishAttempt(DateTimeOffset attemptedAtUtc)
    {
        AttemptCount += 1;
        LastAttemptedAtUtc = attemptedAtUtc;
    }

    private static string CleanRequiredText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        return value.Trim();
    }
}
