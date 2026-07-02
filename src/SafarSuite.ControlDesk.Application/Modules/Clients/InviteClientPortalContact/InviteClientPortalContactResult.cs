namespace SafarSuite.ControlDesk.Application.Modules.Clients.InviteClientPortalContact;

public sealed record InviteClientPortalContactResult(
    Guid InvitationId,
    Guid ClientId,
    Guid ClientContactId,
    string Email,
    string FullName,
    string Role,
    string Status,
    DateTimeOffset InvitedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string? InvitationToken,
    string? InvitationUrl);
