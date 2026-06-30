namespace SafarSuite.ControlDesk.Application.Modules.Clients.AddClientSupportNote;

public sealed record AddClientSupportNoteCommand(
    Guid ClientId,
    string Text,
    string CreatedBy);
