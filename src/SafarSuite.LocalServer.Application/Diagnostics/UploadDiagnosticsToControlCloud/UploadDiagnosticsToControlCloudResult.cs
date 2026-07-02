using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.LocalServer.Application.Diagnostics.UploadDiagnosticsToControlCloud;

public sealed record UploadDiagnosticsToControlCloudResult(
    LocalServerDiagnosticsUploadResponse? Upload,
    string? FailureCode,
    string? Detail)
{
    public bool IsSuccess => Upload is not null;

    public static UploadDiagnosticsToControlCloudResult Success(
        LocalServerDiagnosticsUploadResponse upload)
    {
        return new UploadDiagnosticsToControlCloudResult(
            upload,
            FailureCode: null,
            Detail: null);
    }

    public static UploadDiagnosticsToControlCloudResult Failure(
        string failureCode,
        string detail)
    {
        return new UploadDiagnosticsToControlCloudResult(
            Upload: null,
            failureCode,
            detail);
    }
}
