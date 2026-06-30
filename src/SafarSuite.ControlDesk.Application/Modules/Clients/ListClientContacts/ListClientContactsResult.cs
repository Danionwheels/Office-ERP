namespace SafarSuite.ControlDesk.Application.Modules.Clients.ListClientContacts;

public sealed record ListClientContactsResult(
    Guid ClientId,
    IReadOnlyCollection<ClientContactResult> Contacts);
