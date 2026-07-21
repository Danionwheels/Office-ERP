namespace SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

public enum ControlCloudClientPortalPaymentClaimStatus
{
    PendingVerification = 1,
    Verified = 2,
    Rejected = 3
}

public sealed class ControlCloudClientPortalPaymentClaim
{
    public Guid ClaimId { get; set; }
    public Guid ClientId { get; set; }
    public Guid SubmittedByUserId { get; set; }
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "PKR";
    public string TransferReferenceNumber { get; set; } = "";
    public string NormalizedTransferReferenceNumber { get; set; } = "";
    public Guid? ProofAttachmentId { get; set; }
    public ControlCloudClientPortalPaymentClaimStatus Status { get; set; }
    public DateTimeOffset SubmittedAtUtc { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public Guid? VerifiedPaymentId { get; set; }
    public string? RejectionReason { get; set; }
    public Guid ConcurrencyToken { get; set; }
    public Guid OriginalConcurrencyToken { get; set; }

    public static ControlCloudClientPortalPaymentClaim Create(
        Guid claimId,
        Guid clientId,
        Guid submittedByUserId,
        Guid invoiceId,
        string invoiceNumber,
        decimal amount,
        string currencyCode,
        string transferReferenceNumber,
        Guid? proofAttachmentId,
        DateTimeOffset submittedAtUtc)
    {
        if (claimId == Guid.Empty || clientId == Guid.Empty || submittedByUserId == Guid.Empty
            || invoiceId == Guid.Empty)
        {
            throw new ArgumentException("Payment claim identifiers are required.");
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Payment claim amount must be positive.");
        }

        var reference = CleanRequired(transferReferenceNumber, nameof(transferReferenceNumber), 80);
        var concurrencyToken = Guid.NewGuid();

        return new ControlCloudClientPortalPaymentClaim
        {
            ClaimId = claimId,
            ClientId = clientId,
            SubmittedByUserId = submittedByUserId,
            InvoiceId = invoiceId,
            InvoiceNumber = CleanRequired(invoiceNumber, nameof(invoiceNumber), 80),
            Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            CurrencyCode = NormalizeCurrency(currencyCode),
            TransferReferenceNumber = reference,
            NormalizedTransferReferenceNumber = NormalizeReference(reference),
            ProofAttachmentId = proofAttachmentId,
            Status = ControlCloudClientPortalPaymentClaimStatus.PendingVerification,
            SubmittedAtUtc = submittedAtUtc,
            ConcurrencyToken = concurrencyToken,
            OriginalConcurrencyToken = concurrencyToken
        };
    }

    public void MarkVerified(Guid paymentId, DateTimeOffset reviewedAtUtc)
    {
        if (paymentId == Guid.Empty)
        {
            throw new ArgumentException("Verified payment id is required.", nameof(paymentId));
        }

        if (Status == ControlCloudClientPortalPaymentClaimStatus.Verified
            && VerifiedPaymentId == paymentId)
        {
            return;
        }

        if (Status != ControlCloudClientPortalPaymentClaimStatus.PendingVerification)
        {
            throw new InvalidOperationException("Only pending payment claims can be verified.");
        }

        Status = ControlCloudClientPortalPaymentClaimStatus.Verified;
        VerifiedPaymentId = paymentId;
        ReviewedAtUtc = reviewedAtUtc;
        RejectionReason = null;
        MarkChanged();
    }

    public void Reject(string reason, DateTimeOffset reviewedAtUtc)
    {
        if (Status != ControlCloudClientPortalPaymentClaimStatus.PendingVerification)
        {
            throw new InvalidOperationException("Only pending payment claims can be rejected.");
        }

        RejectionReason = CleanRequired(reason, nameof(reason), 1000);
        Status = ControlCloudClientPortalPaymentClaimStatus.Rejected;
        ReviewedAtUtc = reviewedAtUtc;
        VerifiedPaymentId = null;
        MarkChanged();
    }

    public static string NormalizeReference(string value) =>
        CleanRequired(value, nameof(value), 80).ToUpperInvariant();

    private static string NormalizeCurrency(string value)
    {
        var currency = CleanRequired(value, nameof(value), 3).ToUpperInvariant();
        return currency.Length == 3
            ? currency
            : throw new ArgumentException("Currency code must contain three characters.", nameof(value));
    }

    private static string CleanRequired(string value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        var clean = value.Trim();
        return clean.Length <= maximumLength
            ? clean
            : throw new ArgumentException($"{parameterName} cannot exceed {maximumLength} characters.", parameterName);
    }

    private void MarkChanged()
    {
        if (OriginalConcurrencyToken == Guid.Empty)
        {
            OriginalConcurrencyToken = ConcurrencyToken;
        }

        ConcurrencyToken = Guid.NewGuid();
    }
}
