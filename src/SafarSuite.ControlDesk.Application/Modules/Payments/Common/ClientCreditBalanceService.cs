using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.Common;

public sealed class ClientCreditBalanceService
{
    private readonly IInvoiceRepository _invoices;
    private readonly ICreditNoteRepository _creditNotes;
    private readonly IClientRefundRepository _refunds;
    private readonly IClientCreditApplicationRepository _creditApplications;

    public ClientCreditBalanceService(
        IInvoiceRepository invoices,
        ICreditNoteRepository creditNotes,
        IClientRefundRepository refunds,
        IClientCreditApplicationRepository creditApplications)
    {
        _invoices = invoices;
        _creditNotes = creditNotes;
        _refunds = refunds;
        _creditApplications = creditApplications;
    }

    public async Task<ClientCreditBalance> CalculateAsync(
        ClientId clientId,
        string currencyCode,
        CancellationToken cancellationToken = default)
    {
        var invoices = await _invoices.ListForClientAsync(clientId, cancellationToken: cancellationToken);
        var creditNotes = await _creditNotes.ListForClientAsync(clientId, cancellationToken: cancellationToken);
        var refunds = await _refunds.ListForClientAsync(clientId, cancellationToken: cancellationToken);
        var creditApplications = await _creditApplications.ListForClientAsync(
            clientId,
            cancellationToken: cancellationToken);

        var invoiceBalance = invoices
            .Where(IsPostedReceivableInvoice)
            .Where(invoice => string.Equals(invoice.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
            .Sum(invoice => invoice.BalanceDue.Amount);
        var creditNoteAmount = creditNotes
            .Where(creditNote => string.Equals(creditNote.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
            .Sum(creditNote => creditNote.TotalAmount.Amount);
        var refundAmount = refunds
            .Where(refund => string.Equals(refund.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
            .Where(refund => refund.Status == ClientRefundStatus.Issued)
            .Sum(refund => refund.Amount.Amount);
        var appliedCreditAmount = creditApplications
            .Where(application => string.Equals(application.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
            .Where(application => application.Status == ClientCreditApplicationStatus.Applied)
            .Sum(application => application.Amount.Amount);

        return new ClientCreditBalance(
            currencyCode.Trim().ToUpperInvariant(),
            invoiceBalance,
            creditNoteAmount,
            refundAmount,
            appliedCreditAmount);
    }

    private static bool IsPostedReceivableInvoice(Invoice invoice)
    {
        return invoice.Status is InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid or InvoiceStatus.Paid;
    }
}
