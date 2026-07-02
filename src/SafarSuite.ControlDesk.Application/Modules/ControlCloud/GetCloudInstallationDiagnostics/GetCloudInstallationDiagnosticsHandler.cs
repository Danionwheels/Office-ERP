using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.GetCloudInstallationDiagnostics;

public sealed class GetCloudInstallationDiagnosticsHandler
{
    private readonly IControlCloudInstallationDiagnosticsClient _diagnosticsClient;

    public GetCloudInstallationDiagnosticsHandler(
        IControlCloudInstallationDiagnosticsClient diagnosticsClient)
    {
        _diagnosticsClient = diagnosticsClient;
    }

    public async Task<Result<LocalServerDiagnosticReportResponse>> HandleAsync(
        GetCloudInstallationDiagnosticsQuery query,
        CancellationToken cancellationToken = default)
    {
        var installationId = query.InstallationId.Trim();

        if (query.ClientId == Guid.Empty)
        {
            return Result<LocalServerDiagnosticReportResponse>.Failure(
                ApplicationError.Validation(nameof(query.ClientId), "Client id is required."));
        }

        if (string.IsNullOrWhiteSpace(installationId))
        {
            return Result<LocalServerDiagnosticReportResponse>.Failure(
                ApplicationError.Validation(nameof(query.InstallationId), "Installation id is required."));
        }

        var result = await _diagnosticsClient.GetLatestAsync(
            query.ClientId,
            installationId,
            cancellationToken);

        return result.IsSuccess
            ? Result<LocalServerDiagnosticReportResponse>.Success(result.Report!)
            : Result<LocalServerDiagnosticReportResponse>.Failure(ToApplicationError(result));
    }

    private static ApplicationError ToApplicationError(
        ControlCloudInstallationDiagnosticsClientResult result)
    {
        return result.FailureCode switch
        {
            "InstallationNotFound" => ApplicationError.NotFound(
                "installationId",
                result.Detail ?? "Installation was not found in Control Cloud."),
            "DiagnosticsNotFound" => ApplicationError.NotFound(
                "installationId",
                result.Detail ?? "No diagnostics report has been uploaded for this installation."),
            "InstallationClientMismatch" => ApplicationError.Conflict(
                "installationId",
                result.Detail ?? "Installation belongs to another Control Cloud client."),
            "ControlCloudDiagnosticsNotConfigured" => ApplicationError.Unexpected(
                result.Detail ?? "Control Cloud diagnostics endpoint is not configured."),
            "ControlCloudDiagnosticsUnavailable" => ApplicationError.Unexpected(
                result.Detail ?? "Control Cloud diagnostics are unavailable."),
            "ControlCloudDiagnosticsResponseInvalid" => ApplicationError.Unexpected(
                result.Detail ?? "Control Cloud returned an invalid diagnostics response."),
            _ => ApplicationError.Unexpected(
                result.Detail ?? "Control Cloud diagnostics could not be loaded.")
        };
    }
}
