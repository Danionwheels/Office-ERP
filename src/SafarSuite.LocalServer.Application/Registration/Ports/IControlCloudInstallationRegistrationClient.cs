using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.LocalServer.Application.Registration.Ports;

public interface IControlCloudInstallationRegistrationClient
{
    Task<ControlCloudInstallationRegistrationResult> RegisterAsync(
        Guid clientId,
        string installationId,
        string setupToken,
        string localServerVersion,
        CancellationToken cancellationToken = default);
}

public sealed class ControlCloudInstallationRegistrationResult
{
    private ControlCloudInstallationRegistrationResult(
        LocalServerInstallationRegistrationResponse? registration,
        string? failureCode,
        string? detail)
    {
        Registration = registration;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Registration is not null;

    public LocalServerInstallationRegistrationResponse? Registration { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ControlCloudInstallationRegistrationResult Success(
        LocalServerInstallationRegistrationResponse registration)
    {
        return new ControlCloudInstallationRegistrationResult(
            registration,
            failureCode: null,
            detail: null);
    }

    public static ControlCloudInstallationRegistrationResult Failure(
        string failureCode,
        string detail)
    {
        return new ControlCloudInstallationRegistrationResult(
            registration: null,
            failureCode,
            detail);
    }
}
