using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Billing;

public sealed class CreditNote : Entity<CreditNoteId>
{
    private CreditNote()
    {
        Number = null!;
        TotalAmount = null!;
        CurrencyCode = string.Empty;
        Reason = string.Empty;
    }

    private CreditNote(
        CreditNoteId id,
        InvoiceId invoiceId,
        ClientId clientId,
        ContractId contractId,
        CreditNoteNumber number,
        DateOnly creditDate,
        Money totalAmount,
        string reason,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        InvoiceId = invoiceId;
        ClientId = clientId;
        ContractId = contractId;
        Number = number;
        CreditDate = creditDate;
        TotalAmount = totalAmount;
        CurrencyCode = totalAmount.CurrencyCode;
        Reason = reason;
        CreatedAtUtc = createdAtUtc;
        Status = CreditNoteStatus.Issued;
    }

    public InvoiceId InvoiceId { get; private set; }

    public ClientId ClientId { get; private set; }

    public ContractId ContractId { get; private set; }

    public CreditNoteNumber Number { get; private set; }

    public DateOnly CreditDate { get; private set; }

    public Money TotalAmount { get; private set; }

    public string CurrencyCode { get; private set; }

    public string Reason { get; private set; }

    public CreditNoteStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static CreditNote Create(
        CreditNoteId id,
        Invoice invoice,
        CreditNoteNumber number,
        DateOnly creditDate,
        string reason,
        DateTimeOffset createdAtUtc)
    {
        if (invoice.Status is not (InvoiceStatus.Paid or InvoiceStatus.PartiallyPaid))
        {
            throw new InvalidOperationException("Credit notes can only be issued for paid or partially paid invoices.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Credit note reason is required.", nameof(reason));
        }

        var cleanReason = reason.Trim();

        if (cleanReason.Length > 512)
        {
            throw new ArgumentException("Credit note reason cannot exceed 512 characters.", nameof(reason));
        }

        return new CreditNote(
            id,
            invoice.Id,
            invoice.ClientId,
            invoice.ContractId,
            number,
            creditDate,
            invoice.TotalAmount,
            cleanReason,
            createdAtUtc);
    }
}
