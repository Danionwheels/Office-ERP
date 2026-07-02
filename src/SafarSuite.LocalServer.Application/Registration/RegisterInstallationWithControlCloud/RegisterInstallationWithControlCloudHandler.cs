using SafarSuite.LocalServer.Application.Registration.Ports;

namespace SafarSuite.LocalServer.Application.Registration.RegisterInstallationWithControlCloud;

public sealed class RegisterInstallationWithControlCloudHandler
{
    private readonly IControlCloudInstallationRegistrationClient _cloudClient;

    public RegisterInstallationWithControlCloudHandler(
        IControlCloudInstallationRegistrationClient cloudClient)
    {
        _cloudClient = cloudClient;
    }

    public async Task<RegisterInstallationWithControlCloudResult> HandleAsync(
        RegisterInstallationWithControlCloudCommand command,
        CancellationToken cancellationToken = default)
    {
        var installationId = command.InstallationId.Trim();

        if (command.ClientId == Guid.Empty)
        {
            return RegisterInstallationWithControlCloudResult.Failure(
                "ClientIdRequired",
                "Client id is required before registering with Control Cloud.");
        }

        if (installationId.Length == 0)
        {
            return RegisterInstallationWithControlCloudResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before registering with Control Cloud.");
        }

        if (string.IsNullOrWhiteSpace(command.SetupToken))
        {
            return RegisterInstallationWithControlCloudResult.Failure(
                "SetupTokenRequired",
                "Setup token is required before registering with Control Cloud.");
        }

        var registration = await _cloudClient.RegisterAsync(
            command.ClientId,
            installationId,
            command.SetupToken,
            string.IsNullOrWhiteSpace(command.LocalServerVersion)
                ? "Unknown"
                : command.LocalServerVersion.Trim(),
            cancellationToken);

        return registration.IsSuccess
            ? RegisterInstallationWithControlCloudResult.Success(registration.Registration!)
            : RegisterInstallationWithControlCloudResult.Failure(
                registration.FailureCode ?? "ControlCloudRegistrationFailed",
                registration.Detail ?? "Control Cloud registration failed.");
    }
}
