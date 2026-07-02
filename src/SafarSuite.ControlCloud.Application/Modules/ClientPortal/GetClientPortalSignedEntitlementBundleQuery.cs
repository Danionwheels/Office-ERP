namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed record GetClientPortalSignedEntitlementBundleQuery(
    Guid ClientId,
    string? InstallationId);
