namespace SafarSuite.ControlDesk.Application.Modules.Clients.RevokeClientPortalInvitation;

public sealed record RevokeClientPortalInvitationCommand(
    Guid ClientId,
    Guid InvitationId,
    string RevokedBy);
