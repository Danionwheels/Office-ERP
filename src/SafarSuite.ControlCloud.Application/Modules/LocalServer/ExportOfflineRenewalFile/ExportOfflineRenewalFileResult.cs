using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.ExportOfflineRenewalFile;

public sealed record ExportOfflineRenewalFileResult(
    ControlCloudOfflineRenewalFileResponse? RenewalFile,
    string? FailureCode,
    string? Detail)
{
    public bool IsSuccess => RenewalFile is not null;

    public static ExportOfflineRenewalFileResult Success(
        ControlCloudOfflineRenewalFileResponse renewalFile)
    {
        return new ExportOfflineRenewalFileResult(
            renewalFile,
            FailureCode: null,
            Detail: null);
    }

    public static ExportOfflineRenewalFileResult Failure(
        string failureCode,
        string detail)
    {
        return new ExportOfflineRenewalFileResult(
            RenewalFile: null,
            failureCode,
            detail);
    }
}
