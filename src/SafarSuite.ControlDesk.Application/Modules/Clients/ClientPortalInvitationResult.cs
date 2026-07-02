namespace SafarSuite.ControlDesk.Application.Modules.Clients;

public sealed record ClientPortalInvitationResult(
    Guid InvitationId,
    Guid ClientId,
    string Email,
    string FullName,
    string Role,
    string Status,
    DateTimeOffset InvitedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string? InvitationToken,
    string? InvitationUrl);
