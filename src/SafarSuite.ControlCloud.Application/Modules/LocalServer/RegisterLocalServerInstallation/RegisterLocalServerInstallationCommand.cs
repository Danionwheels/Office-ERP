namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.RegisterLocalServerInstallation;

public sealed record RegisterLocalServerInstallationCommand(
    Guid ClientId,
    string InstallationId,
    string SetupToken,
    string LocalServerVersion);
