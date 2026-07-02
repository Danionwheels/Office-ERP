namespace SafarSuite.ControlDesk.Application.Modules.Clients.InviteClientPortalContact;

public sealed record InviteClientPortalContactCommand(
    Guid ClientId,
    Guid ClientContactId,
    string? PortalRole,
    int ExpiresInDays,
    string CreatedBy);
