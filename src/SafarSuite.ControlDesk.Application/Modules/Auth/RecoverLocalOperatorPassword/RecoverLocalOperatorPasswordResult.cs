namespace SafarSuite.ControlDesk.Application.Modules.Auth.RecoverLocalOperatorPassword;

public sealed record RecoverLocalOperatorPasswordResult(
    Guid OperatorId,
    string Email,
    long SecurityVersion,
    DateTimeOffset RecoveredAtUtc,
    string Actor,
    string Reason);
