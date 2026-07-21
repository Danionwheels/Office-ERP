using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Payments;

public sealed class PortalPaymentClaim : Entity<PortalPaymentClaimId>
{
    private PortalPaymentClaim()
    {
        InvoiceNumber = string.Empty;
        Amount = null!;
        TransferReferenceNumber = string.Empty;
    }

    private PortalPaymentClaim(
        PortalPaymentClaimId id,
        ClientId clientId,
        InvoiceId invoiceId,
        string invoiceNumber,
        Money amount,
        string transferReferenceNumber,
        Guid? proofAttachmentId,
        string? proofFileName,
        string? proofContentType,
        long? proofSizeBytes,
        DateTimeOffset? proofUploadedAtUtc,
        DateTimeOffset submittedAtUtc,
        DateTimeOffset importedAtUtc)
        : base(id)
    {
        ClientId = clientId;
        InvoiceId = invoiceId;
        InvoiceNumber = CleanRequiredText(invoiceNumber, nameof(invoiceNumber), 80);
        Amount = amount;
        TransferReferenceNumber = CleanRequiredText(
            transferReferenceNumber,
            nameof(transferReferenceNumber),
            80);
        ProofAttachmentId = proofAttachmentId;
        ProofFileName = CleanOptionalText(proofFileName, nameof(proofFileName), 255);
        ProofContentType = CleanOptionalText(proofContentType, nameof(proofContentType), 120);
        ProofSizeBytes = proofSizeBytes;
        ProofUploadedAtUtc = proofUploadedAtUtc;
        SubmittedAtUtc = submittedAtUtc;
        ImportedAtUtc = importedAtUtc;
        Status = PortalPaymentClaimStatus.PendingVerification;

        ValidateProofMetadata();
    }

    public ClientId ClientId { get; private set; }

    public InvoiceId InvoiceId { get; private set; }

    public string InvoiceNumber { get; private set; }

    public Money Amount { get; private set; }

    public string TransferReferenceNumber { get; private set; }

    public Guid? ProofAttachmentId { get; private set; }

    public string? ProofFileName { get; private set; }

    public string? ProofContentType { get; private set; }

    public long? ProofSizeBytes { get; private set; }

    public DateTimeOffset? ProofUploadedAtUtc { get; private set; }

    public PortalPaymentClaimStatus Status { get; private set; }

    public DateTimeOffset SubmittedAtUtc { get; private set; }

    public DateTimeOffset ImportedAtUtc { get; private set; }

    public DateTimeOffset? ReviewedAtUtc { get; private set; }

    public PaymentId? VerifiedPaymentId { get; private set; }

    public string? RejectionReason { get; private set; }

    public static PortalPaymentClaim Import(
        PortalPaymentClaimId id,
        ClientId clientId,
        InvoiceId invoiceId,
        string invoiceNumber,
        Money amount,
        string transferReferenceNumber,
        Guid? proofAttachmentId,
        string? proofFileName,
        string? proofContentType,
        long? proofSizeBytes,
        DateTimeOffset? proofUploadedAtUtc,
        DateTimeOffset submittedAtUtc,
        DateTimeOffset importedAtUtc)
    {
        if (amount.Amount <= 0m)
        {
            throw new ArgumentException("Payment claim amount must be positive.", nameof(amount));
        }

        return new PortalPaymentClaim(
            id,
            clientId,
            invoiceId,
            invoiceNumber,
            amount,
            transferReferenceNumber,
            proofAttachmentId,
            proofFileName,
            proofContentType,
            proofSizeBytes,
            proofUploadedAtUtc,
            submittedAtUtc,
            importedAtUtc);
    }

    public void MarkVerified(PaymentId paymentId, DateTimeOffset reviewedAtUtc)
    {
        EnsurePending();
        Status = PortalPaymentClaimStatus.Verified;
        VerifiedPaymentId = paymentId;
        ReviewedAtUtc = reviewedAtUtc;
        RejectionReason = null;
    }

    public void Reject(string reason, DateTimeOffset reviewedAtUtc)
    {
        EnsurePending();
        RejectionReason = CleanRequiredText(reason, nameof(reason), 1000);
        Status = PortalPaymentClaimStatus.Rejected;
        ReviewedAtUtc = reviewedAtUtc;
        VerifiedPaymentId = null;
    }

    private void EnsurePending()
    {
        if (Status != PortalPaymentClaimStatus.PendingVerification)
        {
            throw new InvalidOperationException("Only pending portal payment claims can be reviewed.");
        }
    }

    private void ValidateProofMetadata()
    {
        if (ProofAttachmentId is null)
        {
            ProofFileName = null;
            ProofContentType = null;
            ProofSizeBytes = null;
            ProofUploadedAtUtc = null;
            return;
        }

        if (ProofAttachmentId == Guid.Empty)
        {
            throw new ArgumentException("Proof attachment id cannot be empty.", nameof(ProofAttachmentId));
        }

        if (ProofSizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ProofSizeBytes));
        }
    }

    private static string CleanRequiredText(string value, string parameterName, int maximumLength)
    {
        return CleanOptionalText(value, parameterName, maximumLength)
            ?? throw new ArgumentException($"{parameterName} is required.", parameterName);
    }

    private static string? CleanOptionalText(string? value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var cleaned = value.Trim();

        if (cleaned.Length > maximumLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return cleaned;
    }
}
