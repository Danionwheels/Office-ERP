namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.ListLocalServerBootstrapPackages;

public sealed record ListLocalServerBootstrapPackagesQuery(
    Guid ClientId,
    string InstallationId,
    int Take);
