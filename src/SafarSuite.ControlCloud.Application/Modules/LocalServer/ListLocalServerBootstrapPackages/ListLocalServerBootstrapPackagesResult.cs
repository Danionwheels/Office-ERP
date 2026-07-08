using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.ListLocalServerBootstrapPackages;

public sealed record ListLocalServerBootstrapPackagesResult(
    LocalServerBootstrapPackageRegisterResponse? Response,
    string? FailureCode,
    string? Detail)
{
    public bool IsSuccess => Response is not null;

    public static ListLocalServerBootstrapPackagesResult Success(
        LocalServerBootstrapPackageRegisterResponse response)
    {
        return new ListLocalServerBootstrapPackagesResult(
            response,
            FailureCode: null,
            Detail: null);
    }

    public static ListLocalServerBootstrapPackagesResult Failure(
        string failureCode,
        string detail)
    {
        return new ListLocalServerBootstrapPackagesResult(
            Response: null,
            failureCode,
            detail);
    }
}
