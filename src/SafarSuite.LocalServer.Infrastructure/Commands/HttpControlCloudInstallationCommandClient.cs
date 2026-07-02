using System.Net;
using System.Net.Http.Json;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Commands.Ports;
using SafarSuite.LocalServer.Infrastructure.Entitlements;

namespace SafarSuite.LocalServer.Infrastructure.Commands;

public sealed class HttpControlCloudInstallationCommandClient
    : IControlCloudInstallationCommandClient
{
    private readonly HttpClient _httpClient;
    private readonly ControlCloudEntitlementPullOptions _options;

    public HttpControlCloudInstallationCommandClient(
        HttpClient httpClient,
        ControlCloudEntitlementPullOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<ControlCloudPendingInstallationCommandsResult> GetPendingAsync(
        string installationId,
        CancellationToken cancellationToken = default)
    {
        var cleanInstallationId = installationId.Trim();

        if (cleanInstallationId.Length == 0)
        {
            return ControlCloudPendingInstallationCommandsResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before pulling pending commands.");
        }

        try
        {
            using var response = await _httpClient.GetAsync(
                BuildPendingCommandsUri(cleanInstallationId),
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return ControlCloudPendingInstallationCommandsResult.Failure(
                    "InstallationNotFound",
                    "Control Cloud does not know this installation.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return ControlCloudPendingInstallationCommandsResult.Failure(
                    "ControlCloudCommandPullFailed",
                    $"Control Cloud pending-command pull failed with HTTP {(int)response.StatusCode}.");
            }

            var body = await response.Content.ReadFromJsonAsync<PendingInstallationCommandsResponse>(
                cancellationToken);

            return body is null
                ? ControlCloudPendingInstallationCommandsResult.Failure(
                    "ControlCloudCommandResponseInvalid",
                    "Control Cloud returned an empty pending-command response.")
                : ControlCloudPendingInstallationCommandsResult.Success(body);
        }
        catch (HttpRequestException exception)
        {
            return ControlCloudPendingInstallationCommandsResult.Failure(
                "ControlCloudUnavailable",
                exception.Message);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return ControlCloudPendingInstallationCommandsResult.Failure(
                "ControlCloudTimeout",
                exception.Message);
        }
    }

    public async Task<ControlCloudInstallationCommandAcknowledgementResult> AcknowledgeAsync(
        string installationId,
        Guid commandId,
        AcknowledgeInstallationCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        var cleanInstallationId = installationId.Trim();

        if (cleanInstallationId.Length == 0)
        {
            return ControlCloudInstallationCommandAcknowledgementResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before acknowledging a command.");
        }

        if (commandId == Guid.Empty)
        {
            return ControlCloudInstallationCommandAcknowledgementResult.Failure(
                "CommandIdRequired",
                "Command id is required before acknowledging a command.");
        }

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                BuildAcknowledgementUri(cleanInstallationId, commandId),
                request,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return ControlCloudInstallationCommandAcknowledgementResult.Failure(
                    "CommandNotFound",
                    "Control Cloud does not know this command.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return ControlCloudInstallationCommandAcknowledgementResult.Failure(
                    "ControlCloudCommandAcknowledgementFailed",
                    $"Control Cloud command acknowledgement failed with HTTP {(int)response.StatusCode}.");
            }

            var body = await response.Content.ReadFromJsonAsync<InstallationCommandResponse>(
                cancellationToken);

            return body is null
                ? ControlCloudInstallationCommandAcknowledgementResult.Failure(
                    "ControlCloudCommandResponseInvalid",
                    "Control Cloud returned an empty command acknowledgement response.")
                : ControlCloudInstallationCommandAcknowledgementResult.Success(body);
        }
        catch (HttpRequestException exception)
        {
            return ControlCloudInstallationCommandAcknowledgementResult.Failure(
                "ControlCloudUnavailable",
                exception.Message);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return ControlCloudInstallationCommandAcknowledgementResult.Failure(
                "ControlCloudTimeout",
                exception.Message);
        }
    }

    private Uri BuildPendingCommandsUri(string installationId)
    {
        var baseUri = _httpClient.BaseAddress ?? _options.BaseUrl;

        return new Uri(
            baseUri,
            $"/api/v1/local-server/installations/{Uri.EscapeDataString(installationId)}/commands/pending");
    }

    private Uri BuildAcknowledgementUri(
        string installationId,
        Guid commandId)
    {
        var baseUri = _httpClient.BaseAddress ?? _options.BaseUrl;

        return new Uri(
            baseUri,
            $"/api/v1/local-server/installations/{Uri.EscapeDataString(installationId)}/commands/{commandId:D}/acknowledgement");
    }
}
