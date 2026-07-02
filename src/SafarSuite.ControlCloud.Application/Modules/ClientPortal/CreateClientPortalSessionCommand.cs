namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed record CreateClientPortalSessionCommand(
    Guid ClientId,
    string Email,
    string Password);
