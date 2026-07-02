namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed record RevokeClientPortalInvitationCommand(
    Guid ClientId,
    Guid InvitationId);
