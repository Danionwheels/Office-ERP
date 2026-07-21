namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListCloudInstallationBootstrapPackages;

public sealed record ListCloudInstallationBootstrapPackagesQuery(
    Guid ClientId,
    string InstallationId,
    int Take);
