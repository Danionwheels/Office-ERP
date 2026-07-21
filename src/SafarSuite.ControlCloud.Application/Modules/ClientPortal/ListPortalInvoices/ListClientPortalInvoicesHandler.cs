using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.ListPortalInvoices;

public sealed class ListClientPortalInvoicesHandler
{
    private readonly IControlCloudClientCommercialProjectionRepository _projections;

    public ListClientPortalInvoicesHandler(
        IControlCloudClientCommercialProjectionRepository projections) =>
        _projections = projections;

    public async Task<ClientPortalPaymentOperationResult<IReadOnlyCollection<ClientPortalInvoiceListItem>>> HandleAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        if (clientId == Guid.Empty)
        {
            return ClientPortalPaymentOperationResult<IReadOnlyCollection<ClientPortalInvoiceListItem>>.Failure(
                "ClientIdRequired",
                "Client id is required.");
        }

        var projection = await _projections.GetByClientIdAsync(clientId, cancellationToken);
        if (projection is null)
        {
            return ClientPortalPaymentOperationResult<IReadOnlyCollection<ClientPortalInvoiceListItem>>.Failure(
                "ClientBillingNotFound",
                "No billing projection is available for this client.");
        }

        var invoices = await _projections.ListInvoicesAsync(clientId, cancellationToken);
        var payments = await _projections.ListPaymentsAsync(clientId, cancellationToken: cancellationToken);
        var paidByInvoice = payments
            .Where(payment => !payment.PaymentStatus.Equals("Reversed", StringComparison.OrdinalIgnoreCase)
                && !payment.PaymentStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            .GroupBy(payment => payment.InvoiceId)
            .ToDictionary(group => group.Key, group => group.Sum(payment => payment.Amount));
        var items = invoices
            .OrderByDescending(invoice => invoice.IssueDate)
            .ThenByDescending(invoice => invoice.InvoiceId)
            .Select(invoice => new ClientPortalInvoiceListItem(
                invoice,
                paidByInvoice.GetValueOrDefault(invoice.InvoiceId)))
            .ToArray();

        return ClientPortalPaymentOperationResult<IReadOnlyCollection<ClientPortalInvoiceListItem>>.Success(items);
    }
}
