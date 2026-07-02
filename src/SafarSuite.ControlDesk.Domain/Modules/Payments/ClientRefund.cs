using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Payments;

public sealed class ClientRefund : Entity<ClientRefundId>
{
    private ClientRefund()
    {
        Reference = null!;
        Amount = null!;
        CurrencyCode = string.Empty;
    }

    private ClientRefund(
        ClientRefundId id,
        ClientId clientId,
        PaymentMethod method,
        ClientRefundReference reference,
        Money amount,
        DateOnly refundedOn,
        string? note,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        ClientId = clientId;
        Method = method;
        Reference = reference;
        Amount = amount;
        CurrencyCode = amount.CurrencyCode;
        RefundedOn = refundedOn;
        Note = CleanNote(note);
        CreatedAtUtc = createdAtUtc;
        Status = ClientRefundStatus.Issued;
    }

    public ClientId ClientId { get; private set; }

    public PaymentMethod Method { get; private set; }

    public ClientRefundReference Reference { get; private set; }

    public Money Amount { get; private set; }

    public string CurrencyCode { get; private set; }

    public DateOnly RefundedOn { get; private set; }

    public string? Note { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public ClientRefundStatus Status { get; private set; }

    public static ClientRefund Issue(
        ClientRefundId id,
        ClientId clientId,
        PaymentMethod method,
        ClientRefundReference reference,
        Money amount,
        DateOnly refundedOn,
        string? note,
        DateTimeOffset createdAtUtc)
    {
        if (amount.Amount <= 0)
        {
            throw new ArgumentException("Refund amount must be positive.", nameof(amount));
        }

        return new ClientRefund(id, clientId, method, reference, amount, refundedOn, note, createdAtUtc);
    }

    private static string? CleanNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        var cleanNote = note.Trim();

        if (cleanNote.Length > 1000)
        {
            throw new ArgumentException("Refund note cannot exceed 1000 characters.", nameof(note));
        }

        return cleanNote;
    }
}
