using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Identity.Tests;

public sealed class ClientPortalPaymentClaimDomainTests
{
    [Fact]
    public void Create_normalizes_reference_and_starts_pending()
    {
        var submittedAt = new DateTimeOffset(2026, 7, 14, 8, 30, 0, TimeSpan.Zero);

        var claim = ControlCloudClientPortalPaymentClaim.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "INV-1042",
            1250.456m,
            "pkr",
            "  trx-9081  ",
            null,
            submittedAt);

        Assert.Equal(ControlCloudClientPortalPaymentClaimStatus.PendingVerification, claim.Status);
        Assert.Equal("trx-9081", claim.TransferReferenceNumber);
        Assert.Equal("TRX-9081", claim.NormalizedTransferReferenceNumber);
        Assert.Equal(1250.46m, claim.Amount);
        Assert.Equal("PKR", claim.CurrencyCode);
        Assert.Equal(submittedAt, claim.SubmittedAtUtc);
    }

    [Fact]
    public void Verified_claim_cannot_be_rejected_later()
    {
        var claim = CreateClaim();
        var paymentId = Guid.NewGuid();
        var reviewedAt = DateTimeOffset.UtcNow;

        claim.MarkVerified(paymentId, reviewedAt);

        Assert.Equal(ControlCloudClientPortalPaymentClaimStatus.Verified, claim.Status);
        Assert.Equal(paymentId, claim.VerifiedPaymentId);
        Assert.Equal(reviewedAt, claim.ReviewedAtUtc);
        Assert.Throws<InvalidOperationException>(() => claim.Reject("Not received", reviewedAt.AddMinutes(1)));
    }

    [Fact]
    public void Rejection_requires_a_reason_and_is_terminal()
    {
        var claim = CreateClaim();

        Assert.Throws<ArgumentException>(() => claim.Reject(" ", DateTimeOffset.UtcNow));

        claim.Reject("Reference could not be matched.", DateTimeOffset.UtcNow);

        Assert.Equal(ControlCloudClientPortalPaymentClaimStatus.Rejected, claim.Status);
        Assert.Equal("Reference could not be matched.", claim.RejectionReason);
        Assert.Throws<InvalidOperationException>(() => claim.MarkVerified(Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Attachment_rejects_content_larger_than_five_megabytes()
    {
        var content = new byte[(5 * 1024 * 1024) + 1];

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ControlCloudClientPortalAttachment.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "proof.pdf",
                "application/pdf",
                content,
                new string('a', 64),
                DateTimeOffset.UtcNow));
    }

    private static ControlCloudClientPortalPaymentClaim CreateClaim() =>
        ControlCloudClientPortalPaymentClaim.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "INV-1001",
            100m,
            "PKR",
            "TRX-1001",
            null,
            DateTimeOffset.UtcNow);
}
