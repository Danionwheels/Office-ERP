using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Domain.Registration;

namespace SafarSuite.LocalServer.Application.Registration.RegisterInstallationFromBootstrapBundle;

public sealed record RegisterInstallationFromBootstrapBundleResult(
    LocalServerBootstrapConfiguration? BootstrapConfiguration,
    LocalServerInstallationRegistrationResponse? Registration,
    string? FailureCode,
    string? Detail)
{
    public bool IsSuccess => Registration is not null;

    public static RegisterInstallationFromBootstrapBundleResult Success(
        LocalServerBootstrapConfiguration bootstrapConfiguration,
        LocalServerInstallationRegistrationResponse registration)
    {
        return new RegisterInstallationFromBootstrapBundleResult(
            bootstrapConfiguration,
            registration,
            FailureCode: null,
            Detail: null);
    }

    public static RegisterInstallationFromBootstrapBundleResult Failure(
        LocalServerBootstrapConfiguration? bootstrapConfiguration,
        string failureCode,
        string detail)
    {
        return new RegisterInstallationFromBootstrapBundleResult(
            bootstrapConfiguration,
            Registration: null,
            failureCode,
            detail);
    }
}
