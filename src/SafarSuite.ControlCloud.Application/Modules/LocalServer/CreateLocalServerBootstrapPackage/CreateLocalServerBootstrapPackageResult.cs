using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.CreateLocalServerBootstrapPackage;

public sealed record CreateLocalServerBootstrapPackageResult(
    LocalServerBootstrapPackageResponse? BootstrapPackage,
    string? FailureCode,
    string? Detail)
{
    public bool IsSuccess => BootstrapPackage is not null;

    public static CreateLocalServerBootstrapPackageResult Success(
        LocalServerBootstrapPackageResponse bootstrapPackage)
    {
        return new CreateLocalServerBootstrapPackageResult(
            bootstrapPackage,
            FailureCode: null,
            Detail: null);
    }

    public static CreateLocalServerBootstrapPackageResult Failure(
        string failureCode,
        string detail)
    {
        return new CreateLocalServerBootstrapPackageResult(
            BootstrapPackage: null,
            failureCode,
            detail);
    }
}
