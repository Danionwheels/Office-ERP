namespace SafarSuite.LocalServer.Application.Registration.RegisterInstallationWithControlCloud;

public sealed record RegisterInstallationWithControlCloudCommand(
    Guid ClientId,
    string InstallationId,
    string SetupToken,
    string LocalServerVersion);
