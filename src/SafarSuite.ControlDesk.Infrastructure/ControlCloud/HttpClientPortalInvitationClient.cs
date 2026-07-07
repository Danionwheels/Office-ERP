using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Infrastructure.ControlCloud;

public sealed class HttpClientPortalInvitationClient : IClientPortalInvitationClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<ControlCloudPortalInvitationOptions> _options;
    private readonly IControlCloudProviderAccessCredentialSource _credentialSource;

    public HttpClientPortalInvitationClient(
        HttpClient httpClient,
        IOptions<ControlCloudPortalInvitationOptions> options,
        IControlCloudProviderAccessCredentialSource credentialSource)
    {
        _httpClient = httpClient;
        _options = options;
        _credentialSource = credentialSource;
    }

    public async Task<ClientPortalInvitationListClientResult> ListInvitationsAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(_options.Value.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            return ClientPortalInvitationListClientResult.Failure(
                "ControlCloudInvitationNotConfigured",
                "Control Cloud portal invitation base URL is not configured.");
        }

        if (!HasProviderAccess())
        {
            return ClientPortalInvitationListClientResult.Failure(
                "ControlCloudInvitationNotConfigured",
                "Control Cloud provider invitation key is not configured.");
        }

        var requestUri = new Uri(baseUri, $"/api/v1/client-portal/clients/{clientId:D}/invitations");

        try
        {
            using var message = CreateRequest(HttpMethod.Get, requestUri);
            using var response = await _httpClient.SendAsync(message, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var invitations = await response.Content
                    .ReadFromJsonAsync<ListClientPortalInvitationsResponse>(
                        cancellationToken: cancellationToken);

                return invitations is null
                    ? ClientPortalInvitationListClientResult.Failure(
                        "ControlCloudInvitationResponseInvalid",
                        "Control Cloud returned an empty invitation list response.")
                    : ClientPortalInvitationListClientResult.Success(invitations.Invitations);
            }

            var error = await ReadErrorAsync(response, cancellationToken);

            return ClientPortalInvitationListClientResult.Failure(
                error.Code ?? ToDefaultFailureCode(response.StatusCode),
                error.Detail);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ClientPortalInvitationListClientResult.Failure(
                "ControlCloudInvitationUnavailable",
                "Timed out while listing Control Cloud portal invitations.");
        }
        catch (HttpRequestException exception)
        {
            return ClientPortalInvitationListClientResult.Failure(
                "ControlCloudInvitationUnavailable",
                exception.Message);
        }
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

        if (!HasProviderAccess())
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
            using var message = CreateRequest(HttpMethod.Post, requestUri);
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
                    error.Code ?? ToDefaultFailureCode(response.StatusCode),
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

    public Task<ClientPortalInvitationClientResult> ResendInvitationAsync(
        Guid clientId,
        Guid invitationId,
        int expiresInDays,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        return SendInvitationCommandAsync(
            clientId,
            invitationId,
            "resend",
            new ResendClientPortalInvitationRequest(expiresInDays, createdBy),
            "resending",
            cancellationToken);
    }

    public Task<ClientPortalInvitationClientResult> RevokeInvitationAsync(
        Guid clientId,
        Guid invitationId,
        string revokedBy,
        CancellationToken cancellationToken = default)
    {
        return SendInvitationCommandAsync(
            clientId,
            invitationId,
            "revoke",
            new RevokeClientPortalInvitationRequest(revokedBy),
            "revoking",
            cancellationToken);
    }

    private async Task<ClientPortalInvitationClientResult> SendInvitationCommandAsync<TRequest>(
        Guid clientId,
        Guid invitationId,
        string action,
        TRequest request,
        string actionLabel,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(_options.Value.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            return ClientPortalInvitationClientResult.Failure(
                "ControlCloudInvitationNotConfigured",
                "Control Cloud portal invitation base URL is not configured.");
        }

        if (!HasProviderAccess())
        {
            return ClientPortalInvitationClientResult.Failure(
                "ControlCloudInvitationNotConfigured",
                "Control Cloud provider invitation key is not configured.");
        }

        var requestUri = new Uri(
            baseUri,
            $"/api/v1/client-portal/clients/{clientId:D}/invitations/{invitationId:D}/{action}");

        try
        {
            using var message = CreateRequest(HttpMethod.Post, requestUri);
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

            return ClientPortalInvitationClientResult.Failure(
                error.Code ?? ToDefaultFailureCode(response.StatusCode),
                error.Detail);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ClientPortalInvitationClientResult.Failure(
                "ControlCloudInvitationUnavailable",
                $"Timed out while {actionLabel} the Control Cloud portal invitation.");
        }
        catch (HttpRequestException exception)
        {
            return ClientPortalInvitationClientResult.Failure(
                "ControlCloudInvitationUnavailable",
                exception.Message);
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, Uri requestUri)
    {
        var message = new HttpRequestMessage(method, requestUri);

        if (_credentialSource.TryGetCredential(
                _options.Value.ProviderAccessToken,
                _options.Value.ProviderAccessSecret,
                out var credential))
        {
            message.Headers.TryAddWithoutValidation(
                credential.HeaderName,
                credential.HeaderValue);
        }

        return message;
    }

    private bool HasProviderAccess()
    {
        return _credentialSource.HasCredential(
            _options.Value.ProviderAccessToken,
            _options.Value.ProviderAccessSecret);
    }

    private static string ToDefaultFailureCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.Unauthorized => "ControlCloudInvitationUnauthorized",
            HttpStatusCode.NotFound => "ClientNotFound",
            HttpStatusCode.Conflict => "ControlCloudInvitationConflict",
            _ => "ControlCloudInvitationUnavailable"
        };
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
