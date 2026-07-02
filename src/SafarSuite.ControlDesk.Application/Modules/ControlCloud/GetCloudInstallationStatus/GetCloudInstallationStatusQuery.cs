namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.GetCloudInstallationStatus;

public sealed record GetCloudInstallationStatusQuery(
    Guid ClientId,
    string InstallationId);
