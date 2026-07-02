namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed record AcceptClientPortalInvitationCommand(
    string InvitationToken,
    string Password,
    string? FullName);
