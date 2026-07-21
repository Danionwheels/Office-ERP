namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed record CompleteClientPortalPasswordResetCommand(string ResetToken, string NewPassword);
