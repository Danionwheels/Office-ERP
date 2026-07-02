using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.LocalServer.Application.Entitlements.ImportSignedEntitlementBundle;

public sealed record ImportSignedEntitlementBundleCommand(
    string ExpectedInstallationId,
    ClientPortalSignedEntitlementBundleResponse Bundle);
