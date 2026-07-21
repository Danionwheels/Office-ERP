using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Tests;

public sealed class PortalPaymentClaimTests
{
    [Fact]
    public void Import_creates_pending_claim_and_preserves_proof_metadata()
    {
        var claimId = PortalPaymentClaimId.Create(Guid.NewGuid());
        var clientId = ClientId.Create(Guid.NewGuid());
        var invoiceId = InvoiceId.Create(Guid.NewGuid());
        var proofAttachmentId = Guid.NewGuid();
        var submittedAt = new DateTimeOffset(2026, 7, 14, 8, 30, 0, TimeSpan.Zero);
        var importedAt = submittedAt.AddMinutes(3);
        var proofUploadedAt = submittedAt.AddMinutes(-2);

        var claim = PortalPaymentClaim.Import(
            claimId,
            clientId,
            invoiceId,
            " INV-0042 ",
            Money.Of(1250.126m, "pkr"),
            " BANK-REF-42 ",
            proofAttachmentId,
            " transfer-proof.pdf ",
            " application/pdf ",
            2048,
            proofUploadedAt,
            submittedAt,
            importedAt);

        Assert.Equal(claimId, claim.Id);
        Assert.Equal(clientId, claim.ClientId);
        Assert.Equal(invoiceId, claim.InvoiceId);
        Assert.Equal("INV-0042", claim.InvoiceNumber);
        Assert.Equal(1250.13m, claim.Amount.Amount);
        Assert.Equal("PKR", claim.Amount.CurrencyCode);
        Assert.Equal("BANK-REF-42", claim.TransferReferenceNumber);
        Assert.Equal(proofAttachmentId, claim.ProofAttachmentId);
        Assert.Equal("transfer-proof.pdf", claim.ProofFileName);
        Assert.Equal("application/pdf", claim.ProofContentType);
        Assert.Equal(2048, claim.ProofSizeBytes);
        Assert.Equal(proofUploadedAt, claim.ProofUploadedAtUtc);
        Assert.Equal(submittedAt, claim.SubmittedAtUtc);
        Assert.Equal(importedAt, claim.ImportedAtUtc);
        Assert.Equal(PortalPaymentClaimStatus.PendingVerification, claim.Status);
        Assert.Null(claim.ReviewedAtUtc);
        Assert.Null(claim.VerifiedPaymentId);
        Assert.Null(claim.RejectionReason);
    }

    [Fact]
    public void Import_without_attachment_clears_unlinked_proof_metadata()
    {
        var claim = CreatePendingClaim(
            proofAttachmentId: null,
            proofFileName: "ignored.pdf",
            proofContentType: "application/pdf",
            proofSizeBytes: 2048,
            proofUploadedAtUtc: SubmittedAt.AddMinutes(-1));

        Assert.Null(claim.ProofAttachmentId);
        Assert.Null(claim.ProofFileName);
        Assert.Null(claim.ProofContentType);
        Assert.Null(claim.ProofSizeBytes);
        Assert.Null(claim.ProofUploadedAtUtc);
    }

    [Fact]
    public void Import_rejects_empty_proof_attachment_id()
    {
        Assert.Throws<ArgumentException>(() => CreatePendingClaim(
            proofAttachmentId: Guid.Empty,
            proofFileName: "proof.png",
            proofContentType: "image/png",
            proofSizeBytes: 512,
            proofUploadedAtUtc: SubmittedAt));
    }

    [Fact]
    public void Import_rejects_negative_proof_size()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePendingClaim(
            proofAttachmentId: Guid.NewGuid(),
            proofFileName: "proof.jpg",
            proofContentType: "image/jpeg",
            proofSizeBytes: -1,
            proofUploadedAtUtc: SubmittedAt));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    public void Import_rejects_non_positive_amount(decimal amount)
    {
        Assert.Throws<ArgumentException>(() => PortalPaymentClaim.Import(
            PortalPaymentClaimId.Create(Guid.NewGuid()),
            ClientId.Create(Guid.NewGuid()),
            InvoiceId.Create(Guid.NewGuid()),
            "INV-0042",
            Money.Of(amount, "PKR"),
            "BANK-REF-42",
            null,
            null,
            null,
            null,
            null,
            SubmittedAt,
            SubmittedAt.AddMinutes(1)));
    }

    [Fact]
    public void Mark_verified_is_terminal_and_keeps_the_verified_payment_correlation()
    {
        var claim = CreatePendingClaim();
        var paymentId = PaymentId.Create(Guid.NewGuid());
        var reviewedAt = SubmittedAt.AddHours(1);

        claim.MarkVerified(paymentId, reviewedAt);

        Assert.Equal(PortalPaymentClaimStatus.Verified, claim.Status);
        Assert.Equal(paymentId, claim.VerifiedPaymentId);
        Assert.Equal(reviewedAt, claim.ReviewedAtUtc);
        Assert.Null(claim.RejectionReason);
        Assert.Throws<InvalidOperationException>(() =>
            claim.MarkVerified(PaymentId.Create(Guid.NewGuid()), reviewedAt.AddMinutes(1)));
        Assert.Throws<InvalidOperationException>(() =>
            claim.Reject("late rejection", reviewedAt.AddMinutes(1)));
        Assert.Equal(PortalPaymentClaimStatus.Verified, claim.Status);
        Assert.Equal(paymentId, claim.VerifiedPaymentId);
    }

    [Fact]
    public void Reject_is_terminal_and_records_a_clean_reason()
    {
        var claim = CreatePendingClaim();
        var reviewedAt = SubmittedAt.AddHours(1);

        claim.Reject("  Transfer could not be matched.  ", reviewedAt);

        Assert.Equal(PortalPaymentClaimStatus.Rejected, claim.Status);
        Assert.Equal("Transfer could not be matched.", claim.RejectionReason);
        Assert.Equal(reviewedAt, claim.ReviewedAtUtc);
        Assert.Null(claim.VerifiedPaymentId);
        Assert.Throws<InvalidOperationException>(() =>
            claim.Reject("second rejection", reviewedAt.AddMinutes(1)));
        Assert.Throws<InvalidOperationException>(() =>
            claim.MarkVerified(PaymentId.Create(Guid.NewGuid()), reviewedAt.AddMinutes(1)));
        Assert.Equal(PortalPaymentClaimStatus.Rejected, claim.Status);
        Assert.Null(claim.VerifiedPaymentId);
    }

    private static readonly DateTimeOffset SubmittedAt =
        new(2026, 7, 14, 8, 30, 0, TimeSpan.Zero);

    private static PortalPaymentClaim CreatePendingClaim(
        Guid? proofAttachmentId = null,
        string? proofFileName = null,
        string? proofContentType = null,
        long? proofSizeBytes = null,
        DateTimeOffset? proofUploadedAtUtc = null)
    {
        return PortalPaymentClaim.Import(
            PortalPaymentClaimId.Create(Guid.NewGuid()),
            ClientId.Create(Guid.NewGuid()),
            InvoiceId.Create(Guid.NewGuid()),
            "INV-0042",
            Money.Of(1250m, "PKR"),
            "BANK-REF-42",
            proofAttachmentId,
            proofFileName,
            proofContentType,
            proofSizeBytes,
            proofUploadedAtUtc,
            SubmittedAt,
            SubmittedAt.AddMinutes(3));
    }
}
