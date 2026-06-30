namespace SafarSuite.ControlDesk.Application.Modules.Clients;

public sealed record ClientSupportNoteResult(
    string Text,
    string CreatedBy,
    DateTimeOffset CreatedAtUtc);
