namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.ResetProviderAccessOperatorPassword;

public sealed record ResetProviderAccessOperatorPasswordCommand(
    string UserId,
    string Password,
    string? UpdatedBy);
