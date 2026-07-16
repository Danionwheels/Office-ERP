using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Tests;

public sealed class PaymentPortalClaimCorrelationTests
{
    [Fact]
    public void Record_associates_bank_transfer_with_its_portal_claim()
    {
        var portalClaimId = PortalPaymentClaimId.Create(Guid.NewGuid());

        var payment = Payment.Record(
            PaymentId.Create(Guid.NewGuid()),
            ClientId.Create(Guid.NewGuid()),
            InvoiceId.Create(Guid.NewGuid()),
            PaymentMethod.BankTransfer,
            PaymentReference.Create("BANK-REF-42"),
            Money.Of(1250m, "PKR"),
            new DateOnly(2026, 7, 14),
            new DateTimeOffset(2026, 7, 14, 10, 0, 0, TimeSpan.Zero),
            portalClaimId);

        Assert.Equal(PaymentStatus.PendingReview, payment.Status);
        Assert.Equal(portalClaimId, payment.PortalClaimId);

        payment.Approve("  Verified against provider bank statement.  ");

        Assert.Equal(PaymentStatus.Approved, payment.Status);
        Assert.Equal(portalClaimId, payment.PortalClaimId);
        Assert.Equal("Verified against provider bank statement.", payment.DecisionNote);
    }

    [Fact]
    public void Record_without_portal_claim_keeps_existing_internal_payment_behavior()
    {
        var payment = Payment.Record(
            PaymentId.Create(Guid.NewGuid()),
            ClientId.Create(Guid.NewGuid()),
            InvoiceId.Create(Guid.NewGuid()),
            PaymentMethod.ManualCash,
            PaymentReference.Create("CASH-42"),
            Money.Of(500m, "PKR"),
            new DateOnly(2026, 7, 14),
            new DateTimeOffset(2026, 7, 14, 10, 0, 0, TimeSpan.Zero));

        Assert.Equal(PaymentStatus.Approved, payment.Status);
        Assert.Null(payment.PortalClaimId);
    }
}
