using SafarSuite.ControlCloud.Domain.Modules.InboundControlDesk;

namespace SafarSuite.ControlCloud.Application.Modules.InboundControlDesk;

public sealed record ReceiveControlDeskEnvelopeResult(
    Guid ReceiptId,
    Guid MessageId,
    string MessageType,
    string SubjectType,
    string SubjectId,
    string IdempotencyKey,
    string CloudReference,
    ControlDeskEnvelopeReceiptStatus Status,
    string? Detail,
    string? RejectionCode)
{
    public bool IsSuccess =>
        Status is ControlDeskEnvelopeReceiptStatus.Accepted or ControlDeskEnvelopeReceiptStatus.Duplicate;
}
