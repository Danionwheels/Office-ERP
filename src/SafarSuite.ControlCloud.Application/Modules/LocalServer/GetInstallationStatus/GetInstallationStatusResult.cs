using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.GetInstallationStatus;

public sealed class GetInstallationStatusResult
{
    private GetInstallationStatusResult(
        ControlCloudInstallationStatusResponse? status,
        string? failureCode,
        string? detail)
    {
        Status = status;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Status is not null;

    public ControlCloudInstallationStatusResponse? Status { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static GetInstallationStatusResult Success(
        ControlCloudInstallationStatusResponse status)
    {
        return new GetInstallationStatusResult(
            status,
            failureCode: null,
            detail: null);
    }

    public static GetInstallationStatusResult Failure(
        string failureCode,
        string detail)
    {
        return new GetInstallationStatusResult(
            status: null,
            failureCode,
            detail);
    }
}
