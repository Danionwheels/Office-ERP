using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Payments;

public sealed class ClientCreditApplication : Entity<ClientCreditApplicationId>
{
    private ClientCreditApplication()
    {
        Reference = null!;
        Amount = null!;
        CurrencyCode = string.Empty;
    }

    private ClientCreditApplication(
        ClientCreditApplicationId id,
        ClientId clientId,
        InvoiceId invoiceId,
        ClientCreditApplicationReference reference,
        Money amount,
        DateOnly appliedOn,
        string? note,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        ClientId = clientId;
        InvoiceId = invoiceId;
        Reference = reference;
        Amount = amount;
        CurrencyCode = amount.CurrencyCode;
        AppliedOn = appliedOn;
        Note = CleanNote(note);
        CreatedAtUtc = createdAtUtc;
        Status = ClientCreditApplicationStatus.Applied;
    }

    public ClientId ClientId { get; private set; }

    public InvoiceId InvoiceId { get; private set; }

    public ClientCreditApplicationReference Reference { get; private set; }

    public Money Amount { get; private set; }

    public string CurrencyCode { get; private set; }

    public DateOnly AppliedOn { get; private set; }

    public string? Note { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public ClientCreditApplicationStatus Status { get; private set; }

    public static ClientCreditApplication Apply(
        ClientCreditApplicationId id,
        ClientId clientId,
        InvoiceId invoiceId,
        ClientCreditApplicationReference reference,
        Money amount,
        DateOnly appliedOn,
        string? note,
        DateTimeOffset createdAtUtc)
    {
        if (amount.Amount <= 0)
        {
            throw new ArgumentException("Applied credit amount must be positive.", nameof(amount));
        }

        return new ClientCreditApplication(
            id,
            clientId,
            invoiceId,
            reference,
            amount,
            appliedOn,
            note,
            createdAtUtc);
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
            throw new ArgumentException("Credit application note cannot exceed 1000 characters.", nameof(note));
        }

        return cleanNote;
    }
}
