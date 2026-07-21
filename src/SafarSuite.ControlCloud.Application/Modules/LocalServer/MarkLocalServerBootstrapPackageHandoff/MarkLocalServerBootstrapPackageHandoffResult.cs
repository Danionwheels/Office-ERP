using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.MarkLocalServerBootstrapPackageHandoff;

public sealed record MarkLocalServerBootstrapPackageHandoffResult(
    LocalServerBootstrapPackageHandoffResponse? Response,
    string? FailureCode,
    string? Detail)
{
    public bool IsSuccess => Response is not null;

    public static MarkLocalServerBootstrapPackageHandoffResult Success(
        LocalServerBootstrapPackageHandoffResponse response)
    {
        return new MarkLocalServerBootstrapPackageHandoffResult(
            response,
            FailureCode: null,
            Detail: null);
    }

    public static MarkLocalServerBootstrapPackageHandoffResult Failure(
        string failureCode,
        string detail)
    {
        return new MarkLocalServerBootstrapPackageHandoffResult(
            Response: null,
            failureCode,
            detail);
    }
}
