using System.Net;
using System.Net.Http.Json;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Registration.Ports;
using SafarSuite.LocalServer.Infrastructure.Entitlements;

namespace SafarSuite.LocalServer.Infrastructure.Registration;

public sealed class HttpControlCloudInstallationRegistrationClient
    : IControlCloudInstallationRegistrationClient
{
    private readonly HttpClient _httpClient;
    private readonly ControlCloudEntitlementPullOptions _options;

    public HttpControlCloudInstallationRegistrationClient(
        HttpClient httpClient,
        ControlCloudEntitlementPullOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<ControlCloudInstallationRegistrationResult> RegisterAsync(
        Guid clientId,
        string installationId,
        string setupToken,
        string localServerVersion,
        CancellationToken cancellationToken = default)
    {
        var cleanInstallationId = installationId.Trim();

        if (clientId == Guid.Empty)
        {
            return ControlCloudInstallationRegistrationResult.Failure(
                "ClientIdRequired",
                "Client id is required before registering with Control Cloud.");
        }

        if (cleanInstallationId.Length == 0)
        {
            return ControlCloudInstallationRegistrationResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before registering with Control Cloud.");
        }

        var requestUri = BuildRequestUri(cleanInstallationId);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                requestUri,
                new RegisterLocalServerInstallationRequest(
                    clientId,
                    setupToken,
                    localServerVersion),
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return ControlCloudInstallationRegistrationResult.Failure(
                    "SetupTokenNotFound",
                    "Control Cloud rejected the setup token.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return ControlCloudInstallationRegistrationResult.Failure(
                    "ControlCloudRegistrationFailed",
                    $"Control Cloud returned HTTP {(int)response.StatusCode}.");
            }

            var registration = await response.Content.ReadFromJsonAsync<LocalServerInstallationRegistrationResponse>(
                cancellationToken);

            return registration is null
                ? ControlCloudInstallationRegistrationResult.Failure(
                    "ControlCloudResponseInvalid",
                    "Control Cloud registration response was empty.")
                : ControlCloudInstallationRegistrationResult.Success(registration);
        }
        catch (HttpRequestException exception)
        {
            return ControlCloudInstallationRegistrationResult.Failure(
                "ControlCloudUnavailable",
                exception.Message);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return ControlCloudInstallationRegistrationResult.Failure(
                "ControlCloudTimeout",
                exception.Message);
        }
    }

    private Uri BuildRequestUri(string installationId)
    {
        var baseUri = _httpClient.BaseAddress ?? _options.BaseUrl;
        var path = $"/api/v1/local-server/installations/{Uri.EscapeDataString(installationId)}/registration";

        return new Uri(baseUri, path);
    }
}
