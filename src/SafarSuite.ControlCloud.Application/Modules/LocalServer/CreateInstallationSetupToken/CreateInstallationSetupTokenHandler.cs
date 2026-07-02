using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.CreateInstallationSetupToken;

public sealed class CreateInstallationSetupTokenHandler
{
    private readonly IControlCloudClientCommercialProjectionRepository _projections;
    private readonly IControlCloudClientInstallationRepository _installations;
    private readonly IControlCloudInstallationSetupTokenRepository _setupTokens;
    private readonly IControlCloudInstallationSetupTokenService _tokenService;
    private readonly IClientPortalAuditRecorder _audit;
    private readonly IControlCloudUnitOfWork _unitOfWork;
    private readonly IControlCloudClock _clock;

    public CreateInstallationSetupTokenHandler(
        IControlCloudClientCommercialProjectionRepository projections,
        IControlCloudClientInstallationRepository installations,
        IControlCloudInstallationSetupTokenRepository setupTokens,
        IControlCloudInstallationSetupTokenService tokenService,
        IClientPortalAuditRecorder audit,
        IControlCloudUnitOfWork unitOfWork,
        IControlCloudClock clock)
    {
        _projections = projections;
        _installations = installations;
        _setupTokens = setupTokens;
        _tokenService = tokenService;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<CreateInstallationSetupTokenResult> HandleAsync(
        CreateInstallationSetupTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        var installationId = NormalizeText(command.InstallationId);

        if (command.ClientId == Guid.Empty)
        {
            return CreateInstallationSetupTokenResult.Failure(
                "ClientIdRequired",
                "Client id is required before creating a setup token.");
        }

        if (installationId is null)
        {
            return CreateInstallationSetupTokenResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before creating a setup token.");
        }

        var projection = await _projections.GetByClientIdAsync(command.ClientId, cancellationToken);

        if (projection is null)
        {
            return CreateInstallationSetupTokenResult.Failure(
                "ClientNotFound",
                "Client is not available in Control Cloud yet.");
        }

        var deploymentMode = NormalizeDeploymentMode(command.DeploymentMode);

        if (!ControlCloudBootstrapModes.IsSupported(deploymentMode))
        {
            return CreateInstallationSetupTokenResult.Failure(
                "BootstrapModeUnsupported",
                $"Bootstrap mode must be '{ControlCloudBootstrapModes.OnlineBootstrap}' or '{ControlCloudBootstrapModes.OfflineAssistedBootstrap}'.");
        }

        var clientDeploymentMode =
            SafarSuiteClientDeploymentModes.NormalizeOrDefault(command.ClientDeploymentMode);

        if (!SafarSuiteClientDeploymentModes.IsSupported(clientDeploymentMode))
        {
            return CreateInstallationSetupTokenResult.Failure(
                "ClientDeploymentModeUnsupported",
                $"Client deployment mode must be '{SafarSuiteClientDeploymentModes.OfflineLocal}', '{SafarSuiteClientDeploymentModes.BranchToHqSync}', '{SafarSuiteClientDeploymentModes.CloudSyncMultiBranch}', or '{SafarSuiteClientDeploymentModes.HostedSaas}'.");
        }

        var siteRole = SafarSuiteDeploymentSiteRoles.NormalizeOrDefault(
            command.SiteRole,
            clientDeploymentMode);

        if (!SafarSuiteDeploymentSiteRoles.IsSupported(siteRole))
        {
            return CreateInstallationSetupTokenResult.Failure(
                "SiteRoleUnsupported",
                $"Site role must be '{SafarSuiteDeploymentSiteRoles.Standalone}', '{SafarSuiteDeploymentSiteRoles.Hq}', '{SafarSuiteDeploymentSiteRoles.Branch}', or '{SafarSuiteDeploymentSiteRoles.Hosted}'.");
        }

        var plainToken = _tokenService.CreateSetupToken();
        var now = _clock.UtcNow;
        var expiresAtUtc = now.AddHours(Math.Clamp(command.ExpiresInHours, 1, 168));

        try
        {
            var result = await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var installation = await _installations.GetByInstallationIdAsync(
                        installationId,
                        token);

                    if (installation is not null
                        && installation.ClientId != command.ClientId)
                    {
                        return CreateInstallationSetupTokenResult.Failure(
                            "InstallationClientMismatch",
                            "Installation id is already bound to another client.");
                    }

                    var setupToken = ControlCloudInstallationSetupToken.Create(
                        Guid.NewGuid(),
                        command.ClientId,
                        installationId,
                        _tokenService.HashSecret(plainToken),
                        NormalizeActor(command.CreatedBy),
                        deploymentMode,
                        clientDeploymentMode,
                        command.SiteId,
                        siteRole,
                        command.ParentSiteId,
                        command.BranchCode,
                        command.SyncTopologyId,
                        now,
                        expiresAtUtc);

                    await _setupTokens.AddAsync(setupToken, token);

                    return CreateInstallationSetupTokenResult.Success(
                        setupToken,
                        plainToken);
                },
                cancellationToken);

            if (result.IsSuccess)
            {
                var setupToken = result.SetupToken!;
                await ControlCloudAuditWriter.TryRecordAsync(
                    _audit,
                    new ClientPortalAuditRecord(
                        Guid.NewGuid(),
                        setupToken.ClientId,
                        InvitationId: null,
                        UserId: null,
                        SubjectEmail: "",
                        ClientPortalAuditEventTypes.SetupTokenCreated,
                        setupToken.CreatedBy,
                        $"Setup token '{setupToken.SetupTokenId}' created for installation '{setupToken.InstallationId}' in '{setupToken.DeploymentMode}' mode. Expires at {setupToken.ExpiresAtUtc:O}.",
                        now),
                    cancellationToken);
            }

            return result;
        }
        catch (ArgumentException exception)
        {
            return CreateInstallationSetupTokenResult.Failure(
                "SetupTokenInvalid",
                exception.Message);
        }
    }

    private static string? NormalizeText(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string NormalizeActor(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "SafarSuite Control Cloud"
            : value.Trim();
    }

    private static string NormalizeDeploymentMode(string value)
    {
        return ControlCloudBootstrapModes.NormalizeOrDefault(value);
    }
}
