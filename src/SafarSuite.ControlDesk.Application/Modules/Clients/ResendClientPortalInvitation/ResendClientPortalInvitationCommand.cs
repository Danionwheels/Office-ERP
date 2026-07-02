namespace SafarSuite.ControlDesk.Application.Modules.Clients.ResendClientPortalInvitation;

public sealed record ResendClientPortalInvitationCommand(
    Guid ClientId,
    Guid InvitationId,
    int ExpiresInDays,
    string CreatedBy);
