using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.LocalServer.Application.Diagnostics.Ports;

public interface IControlCloudDiagnosticsClient
{
    Task<ControlCloudDiagnosticsUploadResult> UploadAsync(
        string installationId,
        UploadLocalServerDiagnosticsRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ControlCloudDiagnosticsUploadResult
{
    private ControlCloudDiagnosticsUploadResult(
        LocalServerDiagnosticsUploadResponse? upload,
        string? failureCode,
        string? detail)
    {
        Upload = upload;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Upload is not null;

    public LocalServerDiagnosticsUploadResponse? Upload { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ControlCloudDiagnosticsUploadResult Success(
        LocalServerDiagnosticsUploadResponse upload)
    {
        return new ControlCloudDiagnosticsUploadResult(
            upload,
            failureCode: null,
            detail: null);
    }

    public static ControlCloudDiagnosticsUploadResult Failure(
        string failureCode,
        string detail)
    {
        return new ControlCloudDiagnosticsUploadResult(
            upload: null,
            failureCode,
            detail);
    }
}
