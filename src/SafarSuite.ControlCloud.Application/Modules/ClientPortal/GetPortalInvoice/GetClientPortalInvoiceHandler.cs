using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.GetPortalInvoice;

public sealed class GetClientPortalInvoiceHandler
{
    private readonly IControlCloudClientCommercialProjectionRepository _projections;

    public GetClientPortalInvoiceHandler(
        IControlCloudClientCommercialProjectionRepository projections) =>
        _projections = projections;

    public async Task<ClientPortalPaymentOperationResult<ClientPortalInvoiceDetail>> HandleAsync(
        Guid clientId,
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        if (clientId == Guid.Empty || invoiceId == Guid.Empty)
        {
            return ClientPortalPaymentOperationResult<ClientPortalInvoiceDetail>.Failure(
                "InvoiceIdRequired",
                "Client and invoice ids are required.");
        }

        var invoice = await _projections.GetInvoiceAsync(clientId, invoiceId, cancellationToken);
        if (invoice is null)
        {
            return ClientPortalPaymentOperationResult<ClientPortalInvoiceDetail>.Failure(
                "PortalInvoiceNotFound",
                "Invoice was not found for this client.");
        }

        var payments = await _projections.ListPaymentsAsync(clientId, invoiceId, cancellationToken);
        var activePayments = payments
            .Where(payment => !payment.PaymentStatus.Equals("Reversed", StringComparison.OrdinalIgnoreCase)
                && !payment.PaymentStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(payment => payment.ReceivedOn)
            .ThenByDescending(payment => payment.PaymentId)
            .ToArray();

        return ClientPortalPaymentOperationResult<ClientPortalInvoiceDetail>.Success(
            new ClientPortalInvoiceDetail(
                invoice,
                activePayments.Sum(payment => payment.Amount),
                activePayments));
    }
}
