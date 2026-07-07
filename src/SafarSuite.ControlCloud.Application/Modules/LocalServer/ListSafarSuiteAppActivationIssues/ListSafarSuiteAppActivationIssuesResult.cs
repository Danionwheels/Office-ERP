using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.ListSafarSuiteAppActivationIssues;

public sealed record ListSafarSuiteAppActivationIssuesResult(
    SafarSuiteAppActivationIssuesResponse? Response,
    string? FailureCode,
    string? Detail)
{
    public bool IsSuccess => Response is not null;

    public static ListSafarSuiteAppActivationIssuesResult Success(
        SafarSuiteAppActivationIssuesResponse response)
    {
        return new ListSafarSuiteAppActivationIssuesResult(
            response,
            FailureCode: null,
            Detail: null);
    }

    public static ListSafarSuiteAppActivationIssuesResult Failure(
        string failureCode,
        string detail)
    {
        return new ListSafarSuiteAppActivationIssuesResult(
            Response: null,
            failureCode,
            detail);
    }
}
