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

    public Money BalanceDue => Status is InvoiceStatus.Cancelled or InvoiceStatus.Void
        ? Money.Zero(CurrencyCode)
        : TotalAmount.Subtract(AmountPaid);

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
        ApplySettlement(amount, "Payment");
    }

    public void ApplyCredit(Money amount)
    {
        ApplySettlement(amount, "Credit application");
    }

    private void ApplySettlement(Money amount, string settlementLabel)
    {
        if (Status is InvoiceStatus.Cancelled or InvoiceStatus.Void)
        {
            throw new InvalidOperationException($"Cannot apply {settlementLabel.ToLowerInvariant()} to a cancelled or void invoice.");
        }

        if (amount.Amount <= 0)
        {
            throw new ArgumentException($"{settlementLabel} amount must be positive.", nameof(amount));
        }

        if (Status is InvoiceStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot apply {settlementLabel.ToLowerInvariant()} to a draft invoice.");
        }

        if (amount.Amount > BalanceDue.Amount)
        {
            throw new InvalidOperationException($"{settlementLabel} cannot exceed invoice balance due.");
        }

        AmountPaid = AmountPaid.Add(amount);

        Status = BalanceDue.Amount <= 0
            ? InvoiceStatus.Paid
            : InvoiceStatus.PartiallyPaid;
    }

    public void RemovePayment(Money amount)
    {
        if (Status is InvoiceStatus.Cancelled or InvoiceStatus.Void)
        {
            throw new InvalidOperationException("Cannot remove payment from a cancelled or void invoice.");
        }

        if (amount.Amount <= 0)
        {
            throw new ArgumentException("Payment amount must be positive.", nameof(amount));
        }

        if (amount.Amount > AmountPaid.Amount)
        {
            throw new InvalidOperationException("Payment reversal cannot exceed the amount paid.");
        }

        AmountPaid = AmountPaid.Subtract(amount);

        Status = AmountPaid.Amount <= 0
            ? InvoiceStatus.Issued
            : BalanceDue.Amount <= 0
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

    public void Void()
    {
        if (Status != InvoiceStatus.Issued)
        {
            throw new InvalidOperationException("Only unpaid issued invoices can be voided.");
        }

        if (AmountPaid.Amount > 0)
        {
            throw new InvalidOperationException("Invoices with payments cannot be voided. Reverse payments or use a credit workflow first.");
        }

        Status = InvoiceStatus.Void;
    }

    private void EnsureDraft()
    {
        if (Status != InvoiceStatus.Draft)
        {
            throw new InvalidOperationException("Only draft invoices can be changed.");
        }
    }
}
