namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed record RequestClientPortalPasswordResetCommand(
    Guid ClientId,
    string Email,
    string ResetLinkBase,
    int ExpiresInMinutes = 30,
    int TokenBytes = 32);
