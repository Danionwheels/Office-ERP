namespace SafarSuite.ControlDesk.Application.Modules.Auth.RecoverLocalOperatorPassword;

public sealed record RecoverLocalOperatorPasswordCommand(
    string TargetOperatorIdOrEmail,
    string NewPassword,
    string Actor,
    string Reason);
