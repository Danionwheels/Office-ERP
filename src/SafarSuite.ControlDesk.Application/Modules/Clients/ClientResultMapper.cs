using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients;

internal static class ClientResultMapper
{
    public static ClientDetailsResult ToDetailsResult(Client client)
    {
        return new ClientDetailsResult(
            client.Id.Value,
            client.Code.Value,
            client.LegalName,
            client.DisplayName,
            client.Status.ToString(),
            client.CreatedAtUtc,
            client.ActivatedAtUtc,
            client.SuspendedAtUtc,
            client.Contacts
                .OrderByDescending(contact => contact.IsPrimary)
                .ThenBy(contact => contact.Role)
                .ThenBy(contact => contact.FullName)
                .Select(ToContactResult)
                .ToArray(),
            client.SupportNotes
                .OrderByDescending(note => note.CreatedAtUtc)
                .Select(ToSupportNoteResult)
                .ToArray());
    }

    public static ClientContactResult ToContactResult(ClientContact contact)
    {
        return new ClientContactResult(
            contact.Id.Value,
            contact.Role.ToString(),
            contact.FullName,
            contact.JobTitle,
            contact.Email,
            contact.Phone,
            contact.IsPrimary,
            contact.CreatedAtUtc);
    }

    public static ClientSupportNoteResult ToSupportNoteResult(SupportNote note)
    {
        return new ClientSupportNoteResult(
            note.Text,
            note.CreatedBy,
            note.CreatedAtUtc);
    }
}
