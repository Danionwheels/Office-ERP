namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.GetInstallationStatus;

public sealed record GetInstallationStatusQuery(
    Guid ClientId,
    string InstallationId);
