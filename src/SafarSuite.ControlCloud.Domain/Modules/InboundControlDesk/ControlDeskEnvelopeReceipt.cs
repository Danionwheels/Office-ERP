namespace SafarSuite.ControlCloud.Domain.Modules.InboundControlDesk;

public sealed class ControlDeskEnvelopeReceipt
{
    private ControlDeskEnvelopeReceipt(
        Guid receiptId,
        Guid messageId,
        string messageType,
        string subjectType,
        string subjectId,
        string sourceSystem,
        string sourceEnvironment,
        string idempotencyKey,
        string signatureKeyId,
        string signatureValue,
        ControlDeskEnvelopeReceiptStatus status,
        string cloudReference,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset preparedAtUtc,
        DateTimeOffset receivedAtUtc,
        string? detail)
    {
        ReceiptId = receiptId;
        MessageId = messageId;
        MessageType = messageType;
        SubjectType = subjectType;
        SubjectId = subjectId;
        SourceSystem = sourceSystem;
        SourceEnvironment = sourceEnvironment;
        IdempotencyKey = idempotencyKey;
        SignatureKeyId = signatureKeyId;
        SignatureValue = signatureValue;
        Status = status;
        CloudReference = cloudReference;
        OccurredAtUtc = occurredAtUtc;
        PreparedAtUtc = preparedAtUtc;
        ReceivedAtUtc = receivedAtUtc;
        Detail = detail;
    }

    public Guid ReceiptId { get; }

    public Guid MessageId { get; }

    public string MessageType { get; }

    public string SubjectType { get; }

    public string SubjectId { get; }

    public string SourceSystem { get; }

    public string SourceEnvironment { get; }

    public string IdempotencyKey { get; }

    public string SignatureKeyId { get; }

    public string SignatureValue { get; }

    public ControlDeskEnvelopeReceiptStatus Status { get; }

    public string CloudReference { get; }

    public DateTimeOffset OccurredAtUtc { get; }

    public DateTimeOffset PreparedAtUtc { get; }

    public DateTimeOffset ReceivedAtUtc { get; }

    public string? Detail { get; }

    public static ControlDeskEnvelopeReceipt Accepted(
        Guid receiptId,
        Guid messageId,
        string messageType,
        string subjectType,
        string subjectId,
        string sourceSystem,
        string sourceEnvironment,
        string idempotencyKey,
        string signatureKeyId,
        string signatureValue,
        string cloudReference,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset preparedAtUtc,
        DateTimeOffset receivedAtUtc)
    {
        return new ControlDeskEnvelopeReceipt(
            receiptId,
            messageId,
            messageType,
            subjectType,
            subjectId,
            sourceSystem,
            sourceEnvironment,
            idempotencyKey,
            signatureKeyId,
            signatureValue,
            ControlDeskEnvelopeReceiptStatus.Accepted,
            cloudReference,
            occurredAtUtc,
            preparedAtUtc,
            receivedAtUtc,
            detail: null);
    }

    public static ControlDeskEnvelopeReceipt Rejected(
        Guid receiptId,
        Guid messageId,
        string messageType,
        string subjectType,
        string subjectId,
        string sourceSystem,
        string sourceEnvironment,
        string idempotencyKey,
        string signatureKeyId,
        string signatureValue,
        string cloudReference,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset preparedAtUtc,
        DateTimeOffset receivedAtUtc,
        string detail)
    {
        return new ControlDeskEnvelopeReceipt(
            receiptId,
            messageId,
            messageType,
            subjectType,
            subjectId,
            sourceSystem,
            sourceEnvironment,
            idempotencyKey,
            signatureKeyId,
            signatureValue,
            ControlDeskEnvelopeReceiptStatus.Rejected,
            cloudReference,
            occurredAtUtc,
            preparedAtUtc,
            receivedAtUtc,
            detail);
    }

    public static ControlDeskEnvelopeReceipt Duplicate(
        Guid receiptId,
        Guid messageId,
        string messageType,
        string subjectType,
        string subjectId,
        string sourceSystem,
        string sourceEnvironment,
        string idempotencyKey,
        string signatureKeyId,
        string signatureValue,
        string cloudReference,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset preparedAtUtc,
        DateTimeOffset receivedAtUtc)
    {
        return new ControlDeskEnvelopeReceipt(
            receiptId,
            messageId,
            messageType,
            subjectType,
            subjectId,
            sourceSystem,
            sourceEnvironment,
            idempotencyKey,
            signatureKeyId,
            signatureValue,
            ControlDeskEnvelopeReceiptStatus.Duplicate,
            cloudReference,
            occurredAtUtc,
            preparedAtUtc,
            receivedAtUtc,
            "Duplicate idempotency key.");
    }
}
