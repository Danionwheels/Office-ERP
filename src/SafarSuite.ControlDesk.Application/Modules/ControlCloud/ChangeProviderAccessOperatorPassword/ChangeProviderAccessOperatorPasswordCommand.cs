namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.ChangeProviderAccessOperatorPassword;

public sealed record ChangeProviderAccessOperatorPasswordCommand(
    string Email,
    string CurrentPassword,
    string NewPassword);
