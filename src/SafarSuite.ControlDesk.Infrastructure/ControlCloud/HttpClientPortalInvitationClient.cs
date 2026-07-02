using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Infrastructure.ControlCloud;

public sealed class HttpClientPortalInvitationClient : IClientPortalInvitationClient
{
    private const string ProviderAccessHeaderName = "X-SafarSuite-Provider-Key";

    private readonly HttpClient _httpClient;
    private readonly IOptions<ControlCloudPortalInvitationOptions> _options;

    public HttpClientPortalInvitationClient(
        HttpClient httpClient,
        IOptions<ControlCloudPortalInvitationOptions> options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<ClientPortalInvitationClientResult> CreateInvitationAsync(
        Guid clientId,
        string email,
        string fullName,
        string role,
        int expiresInDays,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(_options.Value.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            return ClientPortalInvitationClientResult.Failure(
                "ControlCloudInvitationNotConfigured",
                "Control Cloud portal invitation base URL is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.Value.ProviderAccessSecret))
        {
            return ClientPortalInvitationClientResult.Failure(
                "ControlCloudInvitationNotConfigured",
                "Control Cloud provider invitation key is not configured.");
        }

        var request = new CreateClientPortalInvitationRequest(
            clientId,
            email,
            fullName,
            role,
            expiresInDays,
            createdBy);
        var requestUri = new Uri(baseUri, "/api/v1/client-portal/invitations");

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, requestUri);
            message.Headers.TryAddWithoutValidation(
                ProviderAccessHeaderName,
                _options.Value.ProviderAccessSecret.Trim());
            message.Content = JsonContent.Create(request);

            using var response = await _httpClient.SendAsync(message, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var invitation = await response.Content
                    .ReadFromJsonAsync<ClientPortalInvitationResponse>(
                        cancellationToken: cancellationToken);

                return invitation is null
                    ? ClientPortalInvitationClientResult.Failure(
                        "ControlCloudInvitationResponseInvalid",
                        "Control Cloud returned an empty invitation response.")
                    : ClientPortalInvitationClientResult.Success(invitation);
            }

            var error = await ReadErrorAsync(response, cancellationToken);

            return response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => ClientPortalInvitationClientResult.Failure(
                    "ControlCloudInvitationUnauthorized",
                    error.Detail),
                HttpStatusCode.NotFound => ClientPortalInvitationClientResult.Failure(
                    error.Code ?? "ClientNotFound",
                    error.Detail),
                HttpStatusCode.Conflict => ClientPortalInvitationClientResult.Failure(
                    error.Code ?? "PortalUserAlreadyExists",
                    error.Detail),
                _ => ClientPortalInvitationClientResult.Failure(
                    error.Code ?? "ControlCloudInvitationUnavailable",
                    error.Detail)
            };
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ClientPortalInvitationClientResult.Failure(
                "ControlCloudInvitationUnavailable",
                "Timed out while creating the Control Cloud portal invitation.");
        }
        catch (HttpRequestException exception)
        {
            return ClientPortalInvitationClientResult.Failure(
                "ControlCloudInvitationUnavailable",
                exception.Message);
        }
    }

    private static async Task<CloudError> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<CloudError>(
                cancellationToken: cancellationToken);

            if (error is not null)
            {
                return error with
                {
                    Detail = string.IsNullOrWhiteSpace(error.Detail)
                        ? $"Control Cloud returned HTTP {(int)response.StatusCode}."
                        : error.Detail
                };
            }
        }
        catch (InvalidOperationException)
        {
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var detail = string.IsNullOrWhiteSpace(body)
            ? $"Control Cloud returned HTTP {(int)response.StatusCode}."
            : body;

        return new CloudError(null, detail);
    }

    private sealed record CloudError(string? Code, string Detail);
}
