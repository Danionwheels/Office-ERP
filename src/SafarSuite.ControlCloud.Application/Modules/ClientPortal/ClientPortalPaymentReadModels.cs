using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed record ClientPortalBillingSummary(
    decimal TotalOutstanding,
    int UnpaidInvoiceCount,
    DateOnly? LastPaymentDate,
    string CurrencyCode);

public sealed record ClientPortalInvoiceListItem(
    ControlCloudInvoiceProjection Invoice,
    decimal AmountPaid);

public sealed record ClientPortalInvoiceDetail(
    ControlCloudInvoiceProjection Invoice,
    decimal AmountPaid,
    IReadOnlyCollection<ControlCloudPaymentProjection> Payments);

public sealed record ClientPortalPaymentClaimView(
    ControlCloudClientPortalPaymentClaim Claim,
    ControlCloudClientPortalAttachment? ProofAttachment);
