namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

public sealed class ControlCloudClientPortalMailDeliveryEntity
{
    public Guid DeliveryId { get; set; }

    public Guid? ClientId { get; set; }

    public string RecipientEmail { get; set; } = "";

    public string RecipientName { get; set; } = "";

    public string Subject { get; set; } = "";

    public string TextBody { get; set; } = "";

    public string Status { get; set; } = "";

    public int AttemptCount { get; set; }

    public DateTimeOffset NextAttemptAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? LastAttemptedAtUtc { get; set; }

    public DateTimeOffset? SentAtUtc { get; set; }

    public DateTimeOffset? FailedAtUtc { get; set; }

    public string? LastError { get; set; }

    public Guid? LeaseId { get; set; }

    public DateTimeOffset? LeaseExpiresAtUtc { get; set; }
}
