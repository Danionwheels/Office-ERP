namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed record ResendClientPortalInvitationCommand(
    Guid ClientId,
    Guid InvitationId,
    int ExpiresInDays,
    string CreatedBy);
