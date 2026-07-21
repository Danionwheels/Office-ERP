using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.IssueLocalServerFirstManagerSetupToken;

public sealed record IssueLocalServerFirstManagerSetupTokenResult(
    IssueLocalServerFirstManagerSetupTokenResponse? Response,
    string? FailureCode,
    string? Detail)
{
    public bool IsSuccess => Response is not null;

    public static IssueLocalServerFirstManagerSetupTokenResult Success(
        IssueLocalServerFirstManagerSetupTokenResponse response)
    {
        return new IssueLocalServerFirstManagerSetupTokenResult(
            response,
            FailureCode: null,
            Detail: null);
    }

    public static IssueLocalServerFirstManagerSetupTokenResult Failure(
        string failureCode,
        string detail)
    {
        return new IssueLocalServerFirstManagerSetupTokenResult(
            Response: null,
            failureCode,
            detail);
    }
}
