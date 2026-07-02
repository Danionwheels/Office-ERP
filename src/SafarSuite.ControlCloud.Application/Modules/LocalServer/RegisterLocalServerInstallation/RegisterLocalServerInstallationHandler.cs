using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.RegisterLocalServerInstallation;

public sealed class RegisterLocalServerInstallationHandler
{
    private readonly IControlCloudClientInstallationRepository _installations;
    private readonly IControlCloudInstallationSetupTokenRepository _setupTokens;
    private readonly IControlCloudInstallationSetupTokenService _tokenService;
    private readonly IClientPortalAuditRecorder _audit;
    private readonly IControlCloudUnitOfWork _unitOfWork;
    private readonly IControlCloudClock _clock;

    public RegisterLocalServerInstallationHandler(
        IControlCloudClientInstallationRepository installations,
        IControlCloudInstallationSetupTokenRepository setupTokens,
        IControlCloudInstallationSetupTokenService tokenService,
        IClientPortalAuditRecorder audit,
        IControlCloudUnitOfWork unitOfWork,
        IControlCloudClock clock)
    {
        _installations = installations;
        _setupTokens = setupTokens;
        _tokenService = tokenService;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<RegisterLocalServerInstallationResult> HandleAsync(
        RegisterLocalServerInstallationCommand command,
        CancellationToken cancellationToken = default)
    {
        var installationId = NormalizeText(command.InstallationId);

        if (command.ClientId == Guid.Empty)
        {
            return RegisterLocalServerInstallationResult.Failure(
                "ClientIdRequired",
                "Client id is required before registering a local server.");
        }

        if (installationId is null)
        {
            return RegisterLocalServerInstallationResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before registering a local server.");
        }

        if (string.IsNullOrWhiteSpace(command.SetupToken))
        {
            return RegisterLocalServerInstallationResult.Failure(
                "SetupTokenRequired",
                "Setup token is required before registering a local server.");
        }

        var now = _clock.UtcNow;
        var localServerVersion = NormalizeText(command.LocalServerVersion) ?? "Unknown";
        var tokenHash = _tokenService.HashSecret(command.SetupToken);

        try
        {
            var result = await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var setupToken = await _setupTokens.GetByTokenHashAsync(
                        tokenHash,
                        token);

                    if (setupToken is null)
                    {
                        return RegisterLocalServerInstallationResult.Failure(
                            "SetupTokenNotFound",
                            "Setup token is not valid.");
                    }

                    if (setupToken.ClientId != command.ClientId
                        || !string.Equals(setupToken.InstallationId, installationId, StringComparison.Ordinal))
                    {
                        return RegisterLocalServerInstallationResult.Failure(
                            "SetupTokenScopeMismatch",
                            "Setup token is not valid for this client installation.");
                    }

                    if (!setupToken.IsPendingAt(now))
                    {
                        return RegisterLocalServerInstallationResult.Failure(
                            "SetupTokenNotUsable",
                            "Setup token has already been used or has expired.");
                    }

                    var installation = await _installations.GetByInstallationIdAsync(
                        installationId,
                        token);
                    var isNewInstallation = installation is null;

                    if (installation is null)
                    {
                        installation = ControlCloudClientInstallation.Register(
                            command.ClientId,
                            installationId,
                            now,
                            setupToken.DeploymentProfile);
                    }
                    else if (installation.ClientId != command.ClientId)
                    {
                        return RegisterLocalServerInstallationResult.Failure(
                            "InstallationClientMismatch",
                            "Installation id is already bound to another client.");
                    }
                    else
                    {
                        installation.UpdateDeploymentProfile(setupToken.DeploymentProfile);
                    }

                    setupToken.Consume(localServerVersion, now);

                    if (isNewInstallation)
                    {
                        await _installations.AddAsync(installation, token);
                    }
                    else
                    {
                        await _installations.SaveAsync(installation, token);
                    }

                    await _setupTokens.SaveAsync(setupToken, token);

                    return RegisterLocalServerInstallationResult.Success(
                        installation,
                        localServerVersion);
                },
                cancellationToken);

            await RecordRegistrationAuditAsync(
                command.ClientId,
                installationId,
                localServerVersion,
                result,
                now,
                cancellationToken);

            return result;
        }
        catch (ArgumentException exception)
        {
            return RegisterLocalServerInstallationResult.Failure(
                "LocalServerRegistrationInvalid",
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return RegisterLocalServerInstallationResult.Failure(
                "SetupTokenNotUsable",
                exception.Message);
        }
    }

    private static string? NormalizeText(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private async Task RecordRegistrationAuditAsync(
        Guid clientId,
        string installationId,
        string localServerVersion,
        RegisterLocalServerInstallationResult result,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        var eventType = result.IsSuccess
            ? ClientPortalAuditEventTypes.LocalServerRegistrationAccepted
            : ClientPortalAuditEventTypes.LocalServerRegistrationRejected;
        var detail = result.IsSuccess
            ? $"Local server installation '{installationId}' registered with version '{localServerVersion}'."
            : $"Local server installation '{installationId}' registration rejected with code '{result.FailureCode}'.";

        await ControlCloudAuditWriter.TryRecordAsync(
            _audit,
            new ClientPortalAuditRecord(
                Guid.NewGuid(),
                clientId,
                InvitationId: null,
                UserId: null,
                SubjectEmail: "",
                eventType,
                ClientPortalAuditActors.LocalServer,
                detail,
                occurredAtUtc),
            cancellationToken);
    }
}
