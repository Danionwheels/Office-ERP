using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.RevokeSafarSuiteAppActivationIssue;

public sealed record RevokeSafarSuiteAppActivationIssueResult(
    SafarSuiteAppActivationIssueResponse? Issue,
    string? FailureCode,
    string? Detail)
{
    public bool IsSuccess => Issue is not null;

    public static RevokeSafarSuiteAppActivationIssueResult Success(
        SafarSuiteAppActivationIssueResponse issue)
    {
        return new RevokeSafarSuiteAppActivationIssueResult(
            issue,
            FailureCode: null,
            Detail: null);
    }

    public static RevokeSafarSuiteAppActivationIssueResult Failure(
        string failureCode,
        string detail)
    {
        return new RevokeSafarSuiteAppActivationIssueResult(
            Issue: null,
            failureCode,
            detail);
    }
}
