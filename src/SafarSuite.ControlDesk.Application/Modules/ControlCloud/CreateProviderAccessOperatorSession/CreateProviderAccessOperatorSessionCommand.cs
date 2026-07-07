namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.CreateProviderAccessOperatorSession;

public sealed record CreateProviderAccessOperatorSessionCommand(
    string Email,
    string Password,
    string[]? Scopes = null,
    int? ExpiresInMinutes = null,
    string? RecoveryCode = null);
