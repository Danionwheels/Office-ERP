using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;

public interface IControlCloudInstallationDiagnosticsClient
{
    Task<ControlCloudInstallationDiagnosticsClientResult> GetLatestAsync(
        Guid clientId,
        string installationId,
        CancellationToken cancellationToken = default);
}

public sealed class ControlCloudInstallationDiagnosticsClientResult
{
    private ControlCloudInstallationDiagnosticsClientResult(
        LocalServerDiagnosticReportResponse? report,
        string? failureCode,
        string? detail)
    {
        Report = report;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Report is not null;

    public LocalServerDiagnosticReportResponse? Report { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ControlCloudInstallationDiagnosticsClientResult Success(
        LocalServerDiagnosticReportResponse report)
    {
        return new ControlCloudInstallationDiagnosticsClientResult(
            report,
            failureCode: null,
            detail: null);
    }

    public static ControlCloudInstallationDiagnosticsClientResult Failure(
        string failureCode,
        string detail)
    {
        return new ControlCloudInstallationDiagnosticsClientResult(
            report: null,
            failureCode,
            detail);
    }
}
