namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

public sealed class ControlCloudInstallationCommandEntity
{
    public Guid CommandId { get; set; }

    public Guid ClientId { get; set; }

    public string InstallationId { get; set; } = "";

    public long CommandVersion { get; set; }

    public string CommandType { get; set; } = "";

    public string Status { get; set; } = "Pending";

    public string IdempotencyKey { get; set; } = "";

    public string PayloadJson { get; set; } = "{}";

    public string SignatureAlgorithm { get; set; } = "";

    public string SignatureKeyId { get; set; } = "";

    public string PayloadSha256 { get; set; } = "";

    public string SignatureValue { get; set; } = "";

    public DateTimeOffset QueuedAtUtc { get; set; }

    public DateTimeOffset? NotBeforeUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? AcknowledgedAtUtc { get; set; }

    public string? AcknowledgementStatus { get; set; }

    public string? AcknowledgementDetail { get; set; }
}
