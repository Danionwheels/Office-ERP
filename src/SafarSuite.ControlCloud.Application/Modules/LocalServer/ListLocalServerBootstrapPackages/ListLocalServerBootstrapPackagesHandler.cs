using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.ListLocalServerBootstrapPackages;

public sealed class ListLocalServerBootstrapPackagesHandler
{
    private readonly IControlCloudInstallationSetupTokenRepository _setupTokens;
    private readonly IControlCloudClock _clock;

    public ListLocalServerBootstrapPackagesHandler(
        IControlCloudInstallationSetupTokenRepository setupTokens,
        IControlCloudClock clock)
    {
        _setupTokens = setupTokens;
        _clock = clock;
    }

    public async Task<ListLocalServerBootstrapPackagesResult> HandleAsync(
        ListLocalServerBootstrapPackagesQuery query,
        CancellationToken cancellationToken = default)
    {
        var installationId = NormalizeText(query.InstallationId);

        if (query.ClientId == Guid.Empty)
        {
            return ListLocalServerBootstrapPackagesResult.Failure(
                "ClientIdRequired",
                "Client id is required before listing bootstrap packages.");
        }

        if (installationId is null)
        {
            return ListLocalServerBootstrapPackagesResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before listing bootstrap packages.");
        }

        if (query.Take is < 1 or > 200)
        {
            return ListLocalServerBootstrapPackagesResult.Failure(
                "BootstrapPackageTakeInvalid",
                "Take must be between 1 and 200.");
        }

        var setupTokens = await _setupTokens.ListBootstrapPackagesAsync(
            query.ClientId,
            installationId,
            query.Take,
            cancellationToken);
        var now = _clock.UtcNow;

        return ListLocalServerBootstrapPackagesResult.Success(
            new LocalServerBootstrapPackageRegisterResponse(
                setupTokens.Select(setupToken => ToResponse(setupToken, now)).ToArray()));
    }

    private static LocalServerBootstrapPackageSummaryResponse ToResponse(
        ControlCloudInstallationSetupToken setupToken,
        DateTimeOffset now)
    {
        return new LocalServerBootstrapPackageSummaryResponse(
            setupToken.BootstrapPackageId!.Value,
            setupToken.SetupTokenId,
            setupToken.ClientId,
            setupToken.InstallationId,
            ToPackageStatus(setupToken, now),
            setupToken.Status,
            setupToken.CreatedBy,
            setupToken.DeploymentMode,
            ToResponse(setupToken.DeploymentProfile),
            setupToken.CreatedAtUtc,
            setupToken.BootstrapPackageGeneratedAtUtc ?? setupToken.CreatedAtUtc,
            setupToken.ExpiresAtUtc,
            setupToken.ConsumedAtUtc,
            setupToken.ConsumedLocalServerVersion,
            setupToken.PackageLocalServerVersion ?? "",
            setupToken.PackageSafarSuiteAppVersion ?? "",
            setupToken.PackageBundleFileName ?? "",
            setupToken.PackageBundleSha256 ?? "");
    }

    private static string ToPackageStatus(
        ControlCloudInstallationSetupToken setupToken,
        DateTimeOffset now)
    {
        return setupToken.Status == ControlCloudInstallationSetupTokenStatuses.Pending
            && setupToken.ExpiresAtUtc <= now
                ? "Expired"
                : setupToken.Status;
    }

    private static LocalServerDeploymentProfileResponse ToResponse(
        ControlCloudInstallationDeploymentProfile deploymentProfile)
    {
        return new LocalServerDeploymentProfileResponse(
            deploymentProfile.BootstrapMode,
            deploymentProfile.ClientDeploymentMode,
            deploymentProfile.SiteId,
            deploymentProfile.SiteRole,
            deploymentProfile.ParentSiteId,
            deploymentProfile.BranchCode,
            deploymentProfile.SyncTopologyId);
    }

    private static string? NormalizeText(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
