namespace SafarSuite.LocalServer.Application.Entitlements.PullEntitlementFromControlCloud;

public sealed record PullEntitlementFromControlCloudCommand(
    Guid ClientId,
    string InstallationId);
