using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.GetCloudInstallationStatus;

public sealed class GetCloudInstallationStatusHandler
{
    private readonly IControlCloudInstallationStatusClient _statusClient;

    public GetCloudInstallationStatusHandler(
        IControlCloudInstallationStatusClient statusClient)
    {
        _statusClient = statusClient;
    }

    public async Task<Result<ControlCloudInstallationStatusResponse>> HandleAsync(
        GetCloudInstallationStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        var installationId = query.InstallationId.Trim();

        if (query.ClientId == Guid.Empty)
        {
            return Result<ControlCloudInstallationStatusResponse>.Failure(
                ApplicationError.Validation(nameof(query.ClientId), "Client id is required."));
        }

        if (string.IsNullOrWhiteSpace(installationId))
        {
            return Result<ControlCloudInstallationStatusResponse>.Failure(
                ApplicationError.Validation(nameof(query.InstallationId), "Installation id is required."));
        }

        var result = await _statusClient.GetStatusAsync(
            query.ClientId,
            installationId,
            cancellationToken);

        if (result.IsSuccess)
        {
            return Result<ControlCloudInstallationStatusResponse>.Success(result.Status!);
        }

        return Result<ControlCloudInstallationStatusResponse>.Failure(ToApplicationError(result));
    }

    private static ApplicationError ToApplicationError(
        ControlCloudInstallationStatusClientResult result)
    {
        return result.FailureCode switch
        {
            "InstallationNotFound" => ApplicationError.NotFound(
                "installationId",
                result.Detail ?? "Installation was not found in Control Cloud."),
            "InstallationClientMismatch" => ApplicationError.Conflict(
                "installationId",
                result.Detail ?? "Installation belongs to another Control Cloud client."),
            "ControlCloudStatusUnavailable" => ApplicationError.ServiceUnavailable(
                result.Detail ?? "Control Cloud status is unavailable."),
            "ControlCloudStatusNotConfigured" => ApplicationError.ServiceUnavailable(
                result.Detail ?? "Control Cloud status endpoint is not configured."),
            _ => ApplicationError.Unexpected(
                result.Detail ?? "Control Cloud status could not be loaded.")
        };
    }
}
