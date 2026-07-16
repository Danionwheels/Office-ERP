using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Infrastructure.ControlCloud;

public sealed class HttpControlCloudPaymentClaimClient : IControlCloudPaymentClaimClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<ControlCloudStatusOptions> _options;
    private readonly IControlCloudProviderAccessCredentialSource _credentialSource;

    public HttpControlCloudPaymentClaimClient(
        HttpClient httpClient,
        IOptions<ControlCloudStatusOptions> options,
        IControlCloudProviderAccessCredentialSource credentialSource)
    {
        _httpClient = httpClient;
        _options = options;
        _credentialSource = credentialSource;
    }

    public async Task<ControlCloudPaymentClaimListClientResult> ListAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var configurationError = ValidateConfiguration();

        if (configurationError is not null)
        {
            return ControlCloudPaymentClaimListClientResult.Failure(
                configurationError.Value.Code,
                configurationError.Value.Detail);
        }

        try
        {
            using var request = CreateRequest(
                HttpMethod.Get,
                $"payment-claims?clientId={clientId:D}");
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await ReadErrorAsync(response, cancellationToken);
                return ControlCloudPaymentClaimListClientResult.Failure(error.Code, error.Detail);
            }

            var body = await response.Content.ReadFromJsonAsync<ClientPortalPaymentClaimListResponse>(
                cancellationToken: cancellationToken);

            return body is null
                ? ControlCloudPaymentClaimListClientResult.Failure(
                    "ControlCloudPaymentClaimResponseInvalid",
                    "Control Cloud returned an empty payment claim list response.")
                : ControlCloudPaymentClaimListClientResult.Success(body.Claims);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ControlCloudPaymentClaimListClientResult.Failure(
                "ControlCloudPaymentClaimUnavailable",
                "Timed out while reading Control Cloud payment claims.");
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException)
        {
            return ControlCloudPaymentClaimListClientResult.Failure(
                "ControlCloudPaymentClaimUnavailable",
                exception.Message);
        }
    }

    public async Task<ControlCloudPaymentClaimProofClientResult> GetProofAsync(
        Guid claimId,
        CancellationToken cancellationToken = default)
    {
        var configurationError = ValidateConfiguration();

        if (configurationError is not null)
        {
            return ControlCloudPaymentClaimProofClientResult.Failure(
                configurationError.Value.Code,
                configurationError.Value.Detail);
        }

        try
        {
            using var request = CreateRequest(HttpMethod.Get, $"payment-claims/{claimId:D}/proof");
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await ReadErrorAsync(response, cancellationToken);
                return ControlCloudPaymentClaimProofClientResult.Failure(error.Code, error.Detail);
            }

            var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.MediaType
                ?? "application/octet-stream";
            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                ?? "payment-proof";

            return ControlCloudPaymentClaimProofClientResult.Success(content, contentType, fileName);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ControlCloudPaymentClaimProofClientResult.Failure(
                "ControlCloudPaymentClaimUnavailable",
                "Timed out while reading the Control Cloud payment proof.");
        }
        catch (HttpRequestException exception)
        {
            return ControlCloudPaymentClaimProofClientResult.Failure(
                "ControlCloudPaymentClaimUnavailable",
                exception.Message);
        }
    }

    private (string Code, string Detail)? ValidateConfiguration()
    {
        if (!Uri.TryCreate(_options.Value.BaseUrl, UriKind.Absolute, out _))
        {
            return (
                "ControlCloudPaymentClaimNotConfigured",
                "Control Cloud provider-access base URL is not configured.");
        }

        return _credentialSource.HasCredential(
            _options.Value.ProviderAccessToken,
            _options.Value.ProviderAccessSecret)
            ? null
            : (
                "ControlCloudPaymentClaimNotConfigured",
                "Control Cloud provider-access credential is not configured.");
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath)
    {
        var baseUri = new Uri(_options.Value.BaseUrl, UriKind.Absolute);
        var request = new HttpRequestMessage(
            method,
            new Uri(baseUri, $"/api/v1/provider-access/{relativePath}"));

        if (_credentialSource.TryGetCredential(
                _options.Value.ProviderAccessToken,
                _options.Value.ProviderAccessSecret,
                out var credential))
        {
            request.Headers.TryAddWithoutValidation(credential.HeaderName, credential.HeaderValue);
        }

        return request;
    }

    private static async Task<(string Code, string Detail)> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<CloudError>(
                cancellationToken: cancellationToken);

            if (error is not null)
            {
                return (
                    error.Code ?? ToFailureCode(response.StatusCode),
                    string.IsNullOrWhiteSpace(error.Detail)
                        ? $"Control Cloud returned HTTP {(int)response.StatusCode}."
                        : error.Detail);
            }
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return (
            ToFailureCode(response.StatusCode),
            string.IsNullOrWhiteSpace(body)
                ? $"Control Cloud returned HTTP {(int)response.StatusCode}."
                : body);
    }

    private static string ToFailureCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.NotFound => "ControlCloudPaymentClaimNotFound",
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "ControlCloudPaymentClaimAccessDenied",
            _ => "ControlCloudPaymentClaimUnavailable"
        };
    }

    private sealed record CloudError(string? Code, string Detail);
}
