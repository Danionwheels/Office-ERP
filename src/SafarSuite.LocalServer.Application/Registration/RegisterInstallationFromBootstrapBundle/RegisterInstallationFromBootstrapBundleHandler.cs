using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Common;
using SafarSuite.LocalServer.Application.Registration.Ports;
using SafarSuite.LocalServer.Application.Registration.RegisterInstallationWithControlCloud;
using SafarSuite.LocalServer.Domain.Registration;

namespace SafarSuite.LocalServer.Application.Registration.RegisterInstallationFromBootstrapBundle;

public sealed class RegisterInstallationFromBootstrapBundleHandler
{
    private readonly ILocalServerBootstrapBundleVerifier _verifier;
    private readonly ILocalServerBootstrapConfigurationStore _configurationStore;
    private readonly RegisterInstallationWithControlCloudHandler _registrationHandler;
    private readonly ILocalServerClock _clock;

    public RegisterInstallationFromBootstrapBundleHandler(
        ILocalServerBootstrapBundleVerifier verifier,
        ILocalServerBootstrapConfigurationStore configurationStore,
        RegisterInstallationWithControlCloudHandler registrationHandler,
        ILocalServerClock clock)
    {
        _verifier = verifier;
        _configurationStore = configurationStore;
        _registrationHandler = registrationHandler;
        _clock = clock;
    }

    public async Task<RegisterInstallationFromBootstrapBundleResult> HandleAsync(
        RegisterInstallationFromBootstrapBundleCommand command,
        CancellationToken cancellationToken = default)
    {
        var importedAtUtc = _clock.UtcNow;
        var verification = _verifier.Verify(
            command.Bundle,
            importedAtUtc,
            command.ExpectedInstallationId);

        if (!verification.IsValid)
        {
            return RegisterInstallationFromBootstrapBundleResult.Failure(
                bootstrapConfiguration: null,
                verification.FailureCode ?? "BootstrapBundleInvalid",
                verification.Detail ?? "Bootstrap bundle verification failed.");
        }

        var verifiedConfiguration = verification.Configuration!;
        var existingConfiguration = await _configurationStore.GetCurrentAsync(cancellationToken);

        if (IsAlreadyRegistered(existingConfiguration, verifiedConfiguration))
        {
            return RegisterInstallationFromBootstrapBundleResult.Success(
                existingConfiguration!,
                ToRegistrationResponse(existingConfiguration!));
        }

        var configuration = verifiedConfiguration
            .RecordRegistrationAttempt(importedAtUtc);
        await _configurationStore.SaveAsync(configuration, cancellationToken);

        var registration = await _registrationHandler.HandleAsync(
            new RegisterInstallationWithControlCloudCommand(
                configuration.ClientId,
                configuration.InstallationId,
                configuration.SetupToken,
                configuration.LocalServerVersion),
            cancellationToken);

        if (!registration.IsSuccess)
        {
            configuration = configuration.RecordRegistrationFailed(
                registration.FailureCode ?? "ControlCloudRegistrationFailed",
                registration.Detail ?? "Control Cloud registration failed.");
            await _configurationStore.SaveAsync(configuration, cancellationToken);

            return RegisterInstallationFromBootstrapBundleResult.Failure(
                configuration,
                registration.FailureCode ?? "ControlCloudRegistrationFailed",
                registration.Detail ?? "Control Cloud registration failed.");
        }

        configuration = configuration.RecordRegistrationSucceeded(
            registration.Registration!.RegisteredAtUtc);
        await _configurationStore.SaveAsync(configuration, cancellationToken);

        return RegisterInstallationFromBootstrapBundleResult.Success(
            configuration,
            registration.Registration);
    }

    private static bool IsAlreadyRegistered(
        LocalServerBootstrapConfiguration? existingConfiguration,
        LocalServerBootstrapConfiguration verifiedConfiguration)
    {
        return existingConfiguration is not null
            && existingConfiguration.ClientId == verifiedConfiguration.ClientId
            && string.Equals(
                existingConfiguration.InstallationId,
                verifiedConfiguration.InstallationId,
                StringComparison.Ordinal)
            && existingConfiguration.BootstrapPackageId == verifiedConfiguration.BootstrapPackageId
            && string.Equals(
                existingConfiguration.PayloadSha256,
                verifiedConfiguration.PayloadSha256,
                StringComparison.Ordinal)
            && string.Equals(
                existingConfiguration.SignatureValue,
                verifiedConfiguration.SignatureValue,
                StringComparison.Ordinal)
            && string.Equals(
                existingConfiguration.RegistrationStatus,
                LocalServerBootstrapRegistrationStatuses.Registered,
                StringComparison.Ordinal)
            && existingConfiguration.LastRegistrationSucceededAtUtc is not null;
    }

    private static LocalServerInstallationRegistrationResponse ToRegistrationResponse(
        LocalServerBootstrapConfiguration configuration)
    {
        return new LocalServerInstallationRegistrationResponse(
            configuration.ClientId,
            configuration.InstallationId,
            InstallationStatus: "Active",
            configuration.LastRegistrationSucceededAtUtc!.Value,
            configuration.LocalServerVersion,
            new LocalServerDeploymentProfileResponse(
                configuration.DeploymentProfile.BootstrapMode,
                configuration.DeploymentProfile.ClientDeploymentMode,
                configuration.DeploymentProfile.SiteId,
                configuration.DeploymentProfile.SiteRole,
                configuration.DeploymentProfile.ParentSiteId,
                configuration.DeploymentProfile.BranchCode,
                configuration.DeploymentProfile.SyncTopologyId));
    }
}
