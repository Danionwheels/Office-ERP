namespace SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

public sealed class ControlCloudClientPortalMailDelivery
{
    public Guid DeliveryId { get; set; }

    public Guid? ClientId { get; set; }

    public string RecipientEmail { get; set; } = "";

    public string RecipientName { get; set; } = "";

    public string Subject { get; set; } = "";

    public string TextBody { get; set; } = "";

    public string Status { get; set; } = ControlCloudClientPortalMailDeliveryStatuses.Pending;

    public int AttemptCount { get; set; }

    public DateTimeOffset NextAttemptAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? LastAttemptedAtUtc { get; set; }

    public DateTimeOffset? SentAtUtc { get; set; }

    public DateTimeOffset? FailedAtUtc { get; set; }

    public string? LastError { get; set; }

    public Guid? LeaseId { get; set; }

    public DateTimeOffset? LeaseExpiresAtUtc { get; set; }

    public static ControlCloudClientPortalMailDelivery Create(
        Guid deliveryId,
        Guid? clientId,
        string recipientEmail,
        string recipientName,
        string subject,
        string textBody,
        DateTimeOffset createdAtUtc)
    {
        if (deliveryId == Guid.Empty)
        {
            throw new InvalidOperationException("Mail delivery id is required.");
        }

        if (clientId == Guid.Empty)
        {
            throw new InvalidOperationException("Client id cannot be empty when supplied.");
        }

        return new ControlCloudClientPortalMailDelivery
        {
            DeliveryId = deliveryId,
            ClientId = clientId,
            RecipientEmail = NormalizeEmail(recipientEmail),
            RecipientName = NormalizeOptionalText(recipientName, "Recipient name", 180),
            Subject = NormalizeRequiredText(subject, "Mail subject", 300),
            TextBody = NormalizeBody(textBody),
            Status = ControlCloudClientPortalMailDeliveryStatuses.Pending,
            NextAttemptAtUtc = createdAtUtc,
            CreatedAtUtc = createdAtUtc
        };
    }

    public bool IsDueAt(DateTimeOffset nowUtc)
    {
        return (string.Equals(
                    Status,
                    ControlCloudClientPortalMailDeliveryStatuses.Pending,
                    StringComparison.Ordinal)
                && NextAttemptAtUtc <= nowUtc)
            || (string.Equals(
                    Status,
                    ControlCloudClientPortalMailDeliveryStatuses.Processing,
                    StringComparison.Ordinal)
                && LeaseExpiresAtUtc is not null
                && LeaseExpiresAtUtc <= nowUtc);
    }

    public void Claim(
        Guid leaseId,
        DateTimeOffset claimedAtUtc,
        DateTimeOffset leaseExpiresAtUtc)
    {
        if (leaseId == Guid.Empty)
        {
            throw new InvalidOperationException("A non-empty lease id is required to claim mail delivery.");
        }

        if (leaseExpiresAtUtc <= claimedAtUtc)
        {
            throw new InvalidOperationException("Mail delivery lease expiry must be after the claim time.");
        }

        if (!IsDueAt(claimedAtUtc))
        {
            throw new InvalidOperationException("Mail delivery is not due for processing.");
        }

        Status = ControlCloudClientPortalMailDeliveryStatuses.Processing;
        LeaseId = leaseId;
        LeaseExpiresAtUtc = leaseExpiresAtUtc;
    }

    public void MarkSent(Guid leaseId, DateTimeOffset sentAtUtc)
    {
        EnsureActiveLease(leaseId);

        AttemptCount++;
        LastAttemptedAtUtc = sentAtUtc;
        SentAtUtc = sentAtUtc;
        FailedAtUtc = null;
        LastError = null;
        Status = ControlCloudClientPortalMailDeliveryStatuses.Sent;
        LeaseExpiresAtUtc = null;
    }

    public void MarkAttemptFailed(
        Guid leaseId,
        DateTimeOffset attemptedAtUtc,
        string error,
        DateTimeOffset? nextAttemptAtUtc)
    {
        EnsureActiveLease(leaseId);

        if (nextAttemptAtUtc is not null && nextAttemptAtUtc <= attemptedAtUtc)
        {
            throw new InvalidOperationException("The next mail delivery attempt must be scheduled in the future.");
        }

        AttemptCount++;
        LastAttemptedAtUtc = attemptedAtUtc;
        LastError = NormalizeError(error);
        SentAtUtc = null;
        LeaseExpiresAtUtc = null;

        if (nextAttemptAtUtc is null)
        {
            Status = ControlCloudClientPortalMailDeliveryStatuses.Failed;
            FailedAtUtc = attemptedAtUtc;
            return;
        }

        Status = ControlCloudClientPortalMailDeliveryStatuses.Pending;
        NextAttemptAtUtc = nextAttemptAtUtc.Value;
        FailedAtUtc = null;
    }

    private void EnsureActiveLease(Guid leaseId)
    {
        if (!string.Equals(
                Status,
                ControlCloudClientPortalMailDeliveryStatuses.Processing,
                StringComparison.Ordinal)
            || leaseId == Guid.Empty
            || LeaseId != leaseId)
        {
            throw new InvalidOperationException("Mail delivery does not hold the expected processing lease.");
        }
    }

    private static string NormalizeEmail(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length is 0 or > 320
            || !normalized.Contains('@', StringComparison.Ordinal)
            || normalized.IndexOf('@') == 0
            || normalized.LastIndexOf('@') == normalized.Length - 1)
        {
            throw new InvalidOperationException("A valid recipient email is required.");
        }

        return normalized;
    }

    private static string NormalizeRequiredText(string value, string fieldName, int maximumLength)
    {
        var normalized = value.Trim();

        if (normalized.Length == 0)
        {
            throw new InvalidOperationException($"{fieldName} is required.");
        }

        if (normalized.Length > maximumLength)
        {
            throw new InvalidOperationException($"{fieldName} cannot exceed {maximumLength} characters.");
        }

        return normalized;
    }

    private static string NormalizeOptionalText(string value, string fieldName, int maximumLength)
    {
        var normalized = value.Trim();

        if (normalized.Length > maximumLength)
        {
            throw new InvalidOperationException($"{fieldName} cannot exceed {maximumLength} characters.");
        }

        return normalized;
    }

    private static string NormalizeBody(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Mail body is required.");
        }

        if (value.Length > 100_000)
        {
            throw new InvalidOperationException("Mail body cannot exceed 100000 characters.");
        }

        return value;
    }

    private static string NormalizeError(string value)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? "Mail delivery failed without an error detail."
            : value.Trim();

        return normalized.Length <= 2_000
            ? normalized
            : normalized[..2_000];
    }
}

public static class ControlCloudClientPortalMailDeliveryStatuses
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Sent = "Sent";
    public const string Failed = "Failed";
}
