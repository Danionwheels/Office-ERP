using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Clients;

public sealed class Client : Entity<ClientId>
{
    private readonly List<ClientContact> _contacts = [];
    private readonly List<SupportNote> _supportNotes = [];

    private Client(
        ClientId id,
        ClientCode code,
        string legalName,
        string displayName,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        Code = code;
        LegalName = legalName;
        DisplayName = displayName;
        CreatedAtUtc = createdAtUtc;
        Status = ClientStatus.Draft;
    }

    public ClientCode Code { get; }

    public string LegalName { get; private set; }

    public string DisplayName { get; private set; }

    public ClientStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset? ActivatedAtUtc { get; private set; }

    public DateTimeOffset? SuspendedAtUtc { get; private set; }

    public IReadOnlyCollection<ClientContact> Contacts => _contacts.AsReadOnly();

    public IReadOnlyCollection<SupportNote> SupportNotes => _supportNotes.AsReadOnly();

    public static Client Create(
        ClientId id,
        ClientCode code,
        string legalName,
        string? displayName,
        DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(legalName))
        {
            throw new ArgumentException("Client legal name is required.", nameof(legalName));
        }

        var cleanLegalName = legalName.Trim();
        var cleanDisplayName = string.IsNullOrWhiteSpace(displayName)
            ? cleanLegalName
            : displayName.Trim();

        return new Client(id, code, cleanLegalName, cleanDisplayName, createdAtUtc);
    }

    public void Rename(string legalName, string? displayName)
    {
        if (string.IsNullOrWhiteSpace(legalName))
        {
            throw new ArgumentException("Client legal name is required.", nameof(legalName));
        }

        LegalName = legalName.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? LegalName
            : displayName.Trim();
    }

    public void Activate(DateTimeOffset activatedAtUtc)
    {
        Status = ClientStatus.Active;
        ActivatedAtUtc = activatedAtUtc;
        SuspendedAtUtc = null;
    }

    public void Suspend(DateTimeOffset suspendedAtUtc)
    {
        Status = ClientStatus.Suspended;
        SuspendedAtUtc = suspendedAtUtc;
    }

    public void AddContact(ClientContact contact)
    {
        if (contact.IsPrimary)
        {
            foreach (var existingContact in _contacts.Where(existingContact => existingContact.Role == contact.Role))
            {
                existingContact.SetPrimary(false);
            }
        }

        _contacts.Add(contact);
    }

    public void AddSupportNote(SupportNote note)
    {
        _supportNotes.Add(note);
    }
}
