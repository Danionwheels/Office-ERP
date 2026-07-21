using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.GetPortalBillingSummary;

public sealed class GetClientPortalBillingSummaryHandler
{
    private readonly IControlCloudClientCommercialProjectionRepository _projections;

    public GetClientPortalBillingSummaryHandler(
        IControlCloudClientCommercialProjectionRepository projections) =>
        _projections = projections;

    public async Task<ClientPortalPaymentOperationResult<ClientPortalBillingSummary>> HandleAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        if (clientId == Guid.Empty)
        {
            return ClientPortalPaymentOperationResult<ClientPortalBillingSummary>.Failure(
                "ClientIdRequired",
                "Client id is required.");
        }

        var projection = await _projections.GetByClientIdAsync(clientId, cancellationToken);
        if (projection is null)
        {
            return ClientPortalPaymentOperationResult<ClientPortalBillingSummary>.Failure(
                "ClientBillingNotFound",
                "No billing projection is available for this client.");
        }

        var invoices = await _projections.ListInvoicesAsync(clientId, cancellationToken);
        var payments = await _projections.ListPaymentsAsync(clientId, cancellationToken: cancellationToken);
        var openInvoices = invoices
            .Where(invoice => !IsVoid(invoice.InvoiceStatus) && invoice.BalanceDue > 0)
            .ToArray();
        var lastPaymentDate = payments
            .Where(payment => !IsReversedOrRejected(payment.PaymentStatus))
            .Select(payment => (DateOnly?)payment.ReceivedOn)
            .Max();

        return ClientPortalPaymentOperationResult<ClientPortalBillingSummary>.Success(
            new ClientPortalBillingSummary(
                openInvoices.Sum(invoice => invoice.BalanceDue),
                openInvoices.Length,
                lastPaymentDate,
                projection.CurrencyCode));
    }

    private static bool IsVoid(string status) =>
        status.Equals("Void", StringComparison.OrdinalIgnoreCase)
        || status.Equals("Voided", StringComparison.OrdinalIgnoreCase);

    private static bool IsReversedOrRejected(string status) =>
        status.Equals("Reversed", StringComparison.OrdinalIgnoreCase)
        || status.Equals("Rejected", StringComparison.OrdinalIgnoreCase);
}
