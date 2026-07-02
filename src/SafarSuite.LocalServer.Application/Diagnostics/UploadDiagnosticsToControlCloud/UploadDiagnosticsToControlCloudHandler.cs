using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Diagnostics.Ports;

namespace SafarSuite.LocalServer.Application.Diagnostics.UploadDiagnosticsToControlCloud;

public sealed class UploadDiagnosticsToControlCloudHandler
{
    private readonly IControlCloudDiagnosticsClient _cloudClient;

    public UploadDiagnosticsToControlCloudHandler(
        IControlCloudDiagnosticsClient cloudClient)
    {
        _cloudClient = cloudClient;
    }

    public async Task<UploadDiagnosticsToControlCloudResult> HandleAsync(
        UploadDiagnosticsToControlCloudCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Bundle.ClientId == Guid.Empty)
        {
            return UploadDiagnosticsToControlCloudResult.Failure(
                "ClientIdRequired",
                "Client id is required before uploading diagnostics.");
        }

        if (string.IsNullOrWhiteSpace(command.Bundle.InstallationId))
        {
            return UploadDiagnosticsToControlCloudResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before uploading diagnostics.");
        }

        var result = await _cloudClient.UploadAsync(
            command.Bundle.InstallationId,
            new UploadLocalServerDiagnosticsRequest(
                command.Bundle.ClientId,
                NormalizeActor(command.UploadedBy),
                NormalizeReason(command.Reason),
                command.Bundle),
            cancellationToken);

        return result.IsSuccess
            ? UploadDiagnosticsToControlCloudResult.Success(result.Upload!)
            : UploadDiagnosticsToControlCloudResult.Failure(
                result.FailureCode ?? "DiagnosticsUploadFailed",
                result.Detail ?? "Control Cloud did not accept the diagnostics bundle.");
    }

    private static string NormalizeActor(string uploadedBy)
    {
        return string.IsNullOrWhiteSpace(uploadedBy)
            ? "SafarSuite Local Server"
            : uploadedBy.Trim();
    }

    private static string NormalizeReason(string reason)
    {
        return string.IsNullOrWhiteSpace(reason)
            ? "Support diagnostics"
            : reason.Trim();
    }
}
