using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;

public interface IControlCloudInstallationStatusClient
{
    Task<ControlCloudInstallationStatusClientResult> GetStatusAsync(
        Guid clientId,
        string installationId,
        CancellationToken cancellationToken = default);
}

public sealed class ControlCloudInstallationStatusClientResult
{
    private ControlCloudInstallationStatusClientResult(
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

    public static ControlCloudInstallationStatusClientResult Success(
        ControlCloudInstallationStatusResponse status)
    {
        return new ControlCloudInstallationStatusClientResult(
            status,
            failureCode: null,
            detail: null);
    }

    public static ControlCloudInstallationStatusClientResult Failure(
        string failureCode,
        string detail)
    {
        return new ControlCloudInstallationStatusClientResult(
            status: null,
            failureCode,
            detail);
    }
}
