namespace SafarSuite.ControlDesk.Application.Modules.Clients.AddClientContact;

public sealed record AddClientContactCommand(
    Guid ClientId,
    string Role,
    string FullName,
    string? JobTitle,
    string? Email,
    string? Phone,
    bool IsPrimary);
