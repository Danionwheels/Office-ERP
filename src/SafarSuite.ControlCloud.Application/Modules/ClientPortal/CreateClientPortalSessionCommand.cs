namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed record CreateClientPortalSessionCommand(
    Guid ClientId,
    string Email,
    string Password,
    string? TotpCode = null,
    string? RecoveryCode = null);
