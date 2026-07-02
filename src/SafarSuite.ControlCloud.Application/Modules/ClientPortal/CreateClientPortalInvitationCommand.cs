namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed record CreateClientPortalInvitationCommand(
    Guid ClientId,
    string Email,
    string FullName,
    string Role,
    int ExpiresInDays,
    string CreatedBy);
