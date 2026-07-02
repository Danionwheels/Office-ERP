using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.ReceiveInstallationDiagnostics;

public sealed record ReceiveInstallationDiagnosticsResult(
    ControlCloudInstallationDiagnosticReport? Report,
    string? FailureCode,
    string? Detail)
{
    public bool IsSuccess => Report is not null;

    public static ReceiveInstallationDiagnosticsResult Success(
        ControlCloudInstallationDiagnosticReport report)
    {
        return new ReceiveInstallationDiagnosticsResult(
            report,
            FailureCode: null,
            Detail: null);
    }

    public static ReceiveInstallationDiagnosticsResult Failure(
        string failureCode,
        string detail)
    {
        return new ReceiveInstallationDiagnosticsResult(
            Report: null,
            failureCode,
            detail);
    }
}
