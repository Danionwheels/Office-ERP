namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

public sealed class ControlDeskEnvelopeReceiptEntity
{
    public Guid ReceiptId { get; set; }

    public Guid MessageId { get; set; }

    public string MessageType { get; set; } = "";

    public string SubjectType { get; set; } = "";

    public string SubjectId { get; set; } = "";

    public string SourceSystem { get; set; } = "";

    public string SourceEnvironment { get; set; } = "";

    public string IdempotencyKey { get; set; } = "";

    public string SignatureKeyId { get; set; } = "";

    public string SignatureValue { get; set; } = "";

    public string Status { get; set; } = "";

    public string CloudReference { get; set; } = "";

    public DateTimeOffset OccurredAtUtc { get; set; }

    public DateTimeOffset PreparedAtUtc { get; set; }

    public DateTimeOffset ReceivedAtUtc { get; set; }

    public string? Detail { get; set; }
}
