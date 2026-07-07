using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.LocalServer.Application.Commands.GetAppActivationRevocationStatus;

public sealed class GetAppActivationRevocationStatusResult
{
    private GetAppActivationRevocationStatusResult(
        LocalServerAppActivationRevocationStatusResponse? status,
        string? failureCode,
        string? detail)
    {
        Status = status;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Status is not null;

    public LocalServerAppActivationRevocationStatusResponse? Status { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static GetAppActivationRevocationStatusResult Success(
        LocalServerAppActivationRevocationStatusResponse status)
    {
        return new GetAppActivationRevocationStatusResult(status, null, null);
    }

    public static GetAppActivationRevocationStatusResult Failure(
        string failureCode,
        string detail)
    {
        return new GetAppActivationRevocationStatusResult(null, failureCode, detail);
    }
}
