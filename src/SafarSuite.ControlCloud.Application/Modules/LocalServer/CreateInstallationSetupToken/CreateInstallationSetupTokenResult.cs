using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.CreateInstallationSetupToken;

public sealed record CreateInstallationSetupTokenResult(
    ControlCloudInstallationSetupToken? SetupToken,
    string? PlainSetupToken,
    string? FailureCode,
    string? Detail)
{
    public bool IsSuccess => SetupToken is not null && PlainSetupToken is not null;

    public static CreateInstallationSetupTokenResult Success(
        ControlCloudInstallationSetupToken setupToken,
        string plainSetupToken)
    {
        return new CreateInstallationSetupTokenResult(
            setupToken,
            plainSetupToken,
            FailureCode: null,
            Detail: null);
    }

    public static CreateInstallationSetupTokenResult Failure(
        string failureCode,
        string detail)
    {
        return new CreateInstallationSetupTokenResult(
            SetupToken: null,
            PlainSetupToken: null,
            failureCode,
            detail);
    }
}
