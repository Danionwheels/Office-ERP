namespace SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

public sealed record ControlCloudReceiveEnvelopeResponse(
    Guid ReceiptId,
    Guid MessageId,
    string MessageType,
    string SubjectType,
    string SubjectId,
    string IdempotencyKey,
    string CloudReference,
    string Status,
    string? Detail);
