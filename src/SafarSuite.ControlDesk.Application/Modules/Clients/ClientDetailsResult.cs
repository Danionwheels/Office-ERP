namespace SafarSuite.ControlDesk.Application.Modules.Clients;

public sealed record ClientDetailsResult(
    Guid ClientId,
    string Code,
    string LegalName,
    string DisplayName,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ActivatedAtUtc,
    DateTimeOffset? SuspendedAtUtc,
    IReadOnlyCollection<ClientContactResult> Contacts,
    IReadOnlyCollection<ClientSupportNoteResult> SupportNotes);
