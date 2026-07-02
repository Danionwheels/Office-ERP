using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.LocalServer.Application.Diagnostics.CreateLocalServerDiagnosticsBundle;

public sealed record CreateLocalServerDiagnosticsBundleResult(
    LocalServerDiagnosticBundleResponse? Bundle,
    string? FailureCode,
    string? Detail)
{
    public bool IsSuccess => Bundle is not null;

    public static CreateLocalServerDiagnosticsBundleResult Success(
        LocalServerDiagnosticBundleResponse bundle)
    {
        return new CreateLocalServerDiagnosticsBundleResult(
            bundle,
            FailureCode: null,
            Detail: null);
    }

    public static CreateLocalServerDiagnosticsBundleResult Failure(
        string failureCode,
        string detail)
    {
        return new CreateLocalServerDiagnosticsBundleResult(
            Bundle: null,
            failureCode,
            detail);
    }
}
