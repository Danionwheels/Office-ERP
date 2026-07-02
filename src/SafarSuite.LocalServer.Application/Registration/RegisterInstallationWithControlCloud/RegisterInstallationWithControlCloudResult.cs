using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.LocalServer.Application.Registration.RegisterInstallationWithControlCloud;

public sealed record RegisterInstallationWithControlCloudResult(
    LocalServerInstallationRegistrationResponse? Registration,
    string? FailureCode,
    string? Detail)
{
    public bool IsSuccess => Registration is not null;

    public static RegisterInstallationWithControlCloudResult Success(
        LocalServerInstallationRegistrationResponse registration)
    {
        return new RegisterInstallationWithControlCloudResult(
            registration,
            FailureCode: null,
            Detail: null);
    }

    public static RegisterInstallationWithControlCloudResult Failure(
        string failureCode,
        string detail)
    {
        return new RegisterInstallationWithControlCloudResult(
            Registration: null,
            failureCode,
            detail);
    }
}
