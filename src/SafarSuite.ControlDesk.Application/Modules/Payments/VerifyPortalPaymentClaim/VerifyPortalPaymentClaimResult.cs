using SafarSuite.ControlDesk.Application.Modules.Payments.Common;
using SafarSuite.ControlDesk.Application.Modules.Payments.RecordInvoicePayment;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.VerifyPortalPaymentClaim;

public sealed record VerifyPortalPaymentClaimResult(
    PortalPaymentClaimResult Claim,
    RecordInvoicePaymentResult Payment);
