using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Domain.Entitlements;

namespace SafarSuite.LocalServer.Application.Entitlements.ImportSignedEntitlementBundle;

public sealed record ImportSignedEntitlementBundleCommand(
    string ExpectedInstallationId,
    ClientPortalSignedEntitlementBundleResponse Bundle,
    string ImportSource = LocalServerEntitlementImportSources.DirectBundle);
