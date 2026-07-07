using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.IssueSafarSuiteAppActivationToken;

public sealed record IssueSafarSuiteAppActivationTokenResult(
    IssueSafarSuiteAppActivationTokenResponse? Response,
    string? FailureCode,
    string? Detail)
{
    public bool IsSuccess => Response is not null;

    public static IssueSafarSuiteAppActivationTokenResult Success(
        IssueSafarSuiteAppActivationTokenResponse response)
    {
        return new IssueSafarSuiteAppActivationTokenResult(
            response,
            FailureCode: null,
            Detail: null);
    }

    public static IssueSafarSuiteAppActivationTokenResult Failure(
        string failureCode,
        string detail)
    {
        return new IssueSafarSuiteAppActivationTokenResult(
            Response: null,
            failureCode,
            detail);
    }
}
