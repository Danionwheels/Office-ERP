using SafarSuite.ControlDesk.Domain.SharedKernel;

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
        string messageType,
        string subjectType,
        string subjectId,
        string payloadJson,
        DateTimeOffset occurredAtUtc)
        : base(id)
    {
        MessageType = messageType;
        SubjectType = subjectType;
        SubjectId = subjectId;
        PayloadJson = payloadJson;
        OccurredAtUtc = occurredAtUtc;
        Status = CloudOutboxMessageStatus.Pending;
    }

    public string MessageType { get; private set; }

    public string SubjectType { get; private set; }

    public string SubjectId { get; private set; }

    public string PayloadJson { get; private set; }

    public CloudOutboxMessageStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public DateTimeOffset? SentAtUtc { get; private set; }

    public DateTimeOffset? FailedAtUtc { get; private set; }

    public string? FailureReason { get; private set; }

    public static CloudOutboxMessage Create(
        CloudOutboxMessageId id,
        string messageType,
        string subjectType,
        string subjectId,
        string payloadJson,
        DateTimeOffset occurredAtUtc)
    {
        return new CloudOutboxMessage(
            id,
            CleanRequiredText(messageType, nameof(messageType)),
            CleanRequiredText(subjectType, nameof(subjectType)),
            CleanRequiredText(subjectId, nameof(subjectId)),
            CleanRequiredText(payloadJson, nameof(payloadJson)),
            occurredAtUtc);
    }

    public void MarkSent(DateTimeOffset sentAtUtc)
    {
        Status = CloudOutboxMessageStatus.Sent;
        SentAtUtc = sentAtUtc;
        FailedAtUtc = null;
        FailureReason = null;
    }

    public void MarkFailed(string reason, DateTimeOffset failedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Failure reason is required.", nameof(reason));
        }

        Status = CloudOutboxMessageStatus.Failed;
        FailedAtUtc = failedAtUtc;
        FailureReason = reason.Trim();
        AttemptCount += 1;
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
