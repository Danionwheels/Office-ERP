using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.LocalServer.Application.Registration.RegisterInstallationFromBootstrapBundle;

public sealed record RegisterInstallationFromBootstrapBundleCommand(
    LocalServerSignedBootstrapBundleResponse Bundle,
    string? ExpectedInstallationId = null);
