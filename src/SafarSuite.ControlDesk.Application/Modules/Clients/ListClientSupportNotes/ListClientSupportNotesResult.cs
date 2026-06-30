namespace SafarSuite.ControlDesk.Application.Modules.Clients.ListClientSupportNotes;

public sealed record ListClientSupportNotesResult(
    Guid ClientId,
    IReadOnlyCollection<ClientSupportNoteResult> SupportNotes);
