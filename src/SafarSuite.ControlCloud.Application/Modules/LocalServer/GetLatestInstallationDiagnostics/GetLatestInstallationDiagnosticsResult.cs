using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.GetLatestInstallationDiagnostics;

public sealed record GetLatestInstallationDiagnosticsResult(
    ControlCloudInstallationDiagnosticReport? Report,
    string? FailureCode,
    string? Detail)
{
    public bool IsSuccess => Report is not null;

    public static GetLatestInstallationDiagnosticsResult Success(
        ControlCloudInstallationDiagnosticReport report)
    {
        return new GetLatestInstallationDiagnosticsResult(
            report,
            FailureCode: null,
            Detail: null);
    }

    public static GetLatestInstallationDiagnosticsResult Failure(
        string failureCode,
        string detail)
    {
        return new GetLatestInstallationDiagnosticsResult(
            Report: null,
            failureCode,
            detail);
    }
}
