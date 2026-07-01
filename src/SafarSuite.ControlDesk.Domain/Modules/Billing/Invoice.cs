using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Billing;

public sealed class Invoice : Entity<InvoiceId>
{
    private readonly List<InvoiceLine> _lines = [];

    private Invoice()
    {
        Number = null!;
        CurrencyCode = string.Empty;
        AmountPaid = null!;
    }

    private Invoice(
        InvoiceId id,
        ClientId clientId,
        ContractId contractId,
        InvoiceNumber number,
        DateOnly issueDate,
        DateOnly dueDate,
        string currencyCode,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        ClientId = clientId;
        ContractId = contractId;
        Number = number;
        IssueDate = issueDate;
        DueDate = dueDate;
        CurrencyCode = currencyCode;
        CreatedAtUtc = createdAtUtc;
        AmountPaid = Money.Zero(currencyCode);
        Status = InvoiceStatus.Draft;
    }

    public ClientId ClientId { get; private set; }

    public ContractId ContractId { get; private set; }

    public InvoiceNumber Number { get; private set; }

    public DateOnly IssueDate { get; private set; }

    public DateOnly DueDate { get; private set; }

    public string CurrencyCode { get; private set; }

    public InvoiceStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Money AmountPaid { get; private set; }

    public IReadOnlyCollection<InvoiceLine> Lines => _lines.AsReadOnly();

    public Money TotalAmount => _lines.Aggregate(Money.Zero(CurrencyCode), (total, line) => total.Add(line.Amount));

    public Money BalanceDue => TotalAmount.Subtract(AmountPaid);

    public static Invoice Create(
        InvoiceId id,
        ClientId clientId,
        ContractId contractId,
        InvoiceNumber number,
        DateOnly issueDate,
        DateOnly dueDate,
        string currencyCode,
        DateTimeOffset createdAtUtc)
    {
        if (dueDate < issueDate)
        {
            throw new ArgumentException("Invoice due date cannot be before issue date.", nameof(dueDate));
        }

        return new Invoice(id, clientId, contractId, number, issueDate, dueDate, currencyCode, createdAtUtc);
    }

    public void AddLine(InvoiceLine line)
    {
        EnsureDraft();
        _lines.Add(line);
    }

    public void Issue()
    {
        EnsureDraft();

        if (_lines.Count == 0)
        {
            throw new InvalidOperationException("Invoice must have at least one line before issue.");
        }

        Status = InvoiceStatus.Issued;
    }

    public void ApplyPayment(Money amount)
    {
        if (Status is InvoiceStatus.Cancelled or InvoiceStatus.Void)
        {
            throw new InvalidOperationException("Cannot apply payment to a cancelled or void invoice.");
        }

        if (amount.Amount <= 0)
        {
            throw new ArgumentException("Payment amount must be positive.", nameof(amount));
        }

        AmountPaid = AmountPaid.Add(amount);

        Status = BalanceDue.Amount <= 0
            ? InvoiceStatus.Paid
            : InvoiceStatus.PartiallyPaid;
    }

    public void Cancel()
    {
        if (Status == InvoiceStatus.Paid)
        {
            throw new InvalidOperationException("Paid invoices cannot be cancelled.");
        }

        Status = InvoiceStatus.Cancelled;
    }

    private void EnsureDraft()
    {
        if (Status != InvoiceStatus.Draft)
        {
            throw new InvalidOperationException("Only draft invoices can be changed.");
        }
    }
}
