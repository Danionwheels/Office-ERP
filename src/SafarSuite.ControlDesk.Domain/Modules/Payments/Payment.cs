using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Payments;

public sealed class Payment : Entity<PaymentId>
{
    private Payment(
        PaymentId id,
        ClientId clientId,
        InvoiceId invoiceId,
        PaymentMethod method,
        PaymentReference reference,
        Money amount,
        DateOnly receivedOn,
        DateTimeOffset recordedAtUtc)
        : base(id)
    {
        ClientId = clientId;
        InvoiceId = invoiceId;
        Method = method;
        Reference = reference;
        Amount = amount;
        ReceivedOn = receivedOn;
        RecordedAtUtc = recordedAtUtc;
        Status = method == PaymentMethod.BankTransfer ? PaymentStatus.PendingReview : PaymentStatus.Approved;
    }

    public ClientId ClientId { get; }

    public InvoiceId InvoiceId { get; }

    public PaymentMethod Method { get; }

    public PaymentReference Reference { get; }

    public Money Amount { get; }

    public DateOnly ReceivedOn { get; }

    public DateTimeOffset RecordedAtUtc { get; }

    public PaymentStatus Status { get; private set; }

    public string? DecisionNote { get; private set; }

    public static Payment Record(
        PaymentId id,
        ClientId clientId,
        InvoiceId invoiceId,
        PaymentMethod method,
        PaymentReference reference,
        Money amount,
        DateOnly receivedOn,
        DateTimeOffset recordedAtUtc)
    {
        if (amount.Amount <= 0)
        {
            throw new ArgumentException("Payment amount must be positive.", nameof(amount));
        }

        return new Payment(id, clientId, invoiceId, method, reference, amount, receivedOn, recordedAtUtc);
    }

    public void Approve(string? decisionNote = null)
    {
        if (Status == PaymentStatus.Reversed)
        {
            throw new InvalidOperationException("Reversed payments cannot be approved.");
        }

        Status = PaymentStatus.Approved;
        DecisionNote = CleanNote(decisionNote);
    }

    public void Reject(string decisionNote)
    {
        if (string.IsNullOrWhiteSpace(decisionNote))
        {
            throw new ArgumentException("Rejection note is required.", nameof(decisionNote));
        }

        Status = PaymentStatus.Rejected;
        DecisionNote = decisionNote.Trim();
    }

    public void Reverse(string decisionNote)
    {
        if (string.IsNullOrWhiteSpace(decisionNote))
        {
            throw new ArgumentException("Reversal note is required.", nameof(decisionNote));
        }

        if (Status != PaymentStatus.Approved)
        {
            throw new InvalidOperationException("Only approved payments can be reversed.");
        }

        Status = PaymentStatus.Reversed;
        DecisionNote = decisionNote.Trim();
    }

    private static string? CleanNote(string? note)
    {
        return string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }
}
