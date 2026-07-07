using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Infrastructure.ControlCloud;

public sealed class HttpControlCloudProviderAccessClient : IControlCloudProviderAccessClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<ControlCloudStatusOptions> _options;
    private readonly IControlCloudProviderAccessCredentialSource _credentialSource;

    public HttpControlCloudProviderAccessClient(
        HttpClient httpClient,
        IOptions<ControlCloudStatusOptions> options,
        IControlCloudProviderAccessCredentialSource credentialSource)
    {
        _httpClient = httpClient;
        _options = options;
        _credentialSource = credentialSource;
    }

    public async Task<ControlCloudProviderAccessSessionClientResult> CreateOperatorSessionAsync(
        CreateProviderOperatorSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await SendWithoutProviderAccessAsync<
            CreateProviderOperatorSessionRequest,
            ProviderAccessSessionResponse>(
            "operator-sessions",
            request,
            "provider operator session",
            cancellationToken);

        return result.IsSuccess
            ? ControlCloudProviderAccessSessionClientResult.Success(result.Response!)
            : ControlCloudProviderAccessSessionClientResult.Failure(
                result.FailureCode!,
                result.Detail!);
    }

    public async Task<ControlCloudProviderOperatorClientResult> ChangeOperatorPasswordAsync(
        ChangeProviderOperatorPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await SendWithoutProviderAccessAsync<
            ChangeProviderOperatorPasswordRequest,
            ProviderAccessOperatorResponse>(
            "operator-password",
            request,
            "provider operator password",
            cancellationToken);

        return result.IsSuccess
            ? ControlCloudProviderOperatorClientResult.Success(result.Response!)
            : ControlCloudProviderOperatorClientResult.Failure(
                result.FailureCode!,
                result.Detail!);
    }

    public async Task<ControlCloudProviderOperatorsClientResult> ListOperatorsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await GetAsync<ProviderAccessOperatorsResponse>(
            "operators",
            "provider operators",
            cancellationToken);

        return result.IsSuccess
            ? ControlCloudProviderOperatorsClientResult.Success(result.Response!)
            : ControlCloudProviderOperatorsClientResult.Failure(result.FailureCode!, result.Detail!);
    }

    public async Task<ControlCloudProviderOperatorClientResult> CreateOperatorAsync(
        CreateProviderOperatorRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await SendAsync<CreateProviderOperatorRequest, ProviderAccessOperatorResponse>(
            "operators",
            request,
            "provider operator",
            cancellationToken);

        return result.IsSuccess
            ? ControlCloudProviderOperatorClientResult.Success(result.Response!)
            : ControlCloudProviderOperatorClientResult.Failure(result.FailureCode!, result.Detail!);
    }

    public async Task<ControlCloudProviderOperatorClientResult> ResetOperatorPasswordAsync(
        string userId,
        ResetProviderOperatorPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await SendAsync<ResetProviderOperatorPasswordRequest, ProviderAccessOperatorResponse>(
            $"operators/{Uri.EscapeDataString(userId.Trim())}/password",
            request,
            "provider operator password",
            cancellationToken);

        return result.IsSuccess
            ? ControlCloudProviderOperatorClientResult.Success(result.Response!)
            : ControlCloudProviderOperatorClientResult.Failure(result.FailureCode!, result.Detail!);
    }

    public async Task<ControlCloudProviderOperatorClientResult> UpdateOperatorScopesAsync(
        string userId,
        UpdateProviderOperatorScopesRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await SendAsync<UpdateProviderOperatorScopesRequest, ProviderAccessOperatorResponse>(
            $"operators/{Uri.EscapeDataString(userId.Trim())}/scopes",
            request,
            "provider operator scopes",
            cancellationToken);

        return result.IsSuccess
            ? ControlCloudProviderOperatorClientResult.Success(result.Response!)
            : ControlCloudProviderOperatorClientResult.Failure(result.FailureCode!, result.Detail!);
    }

    public async Task<ControlCloudProviderOperatorClientResult> UpdateOperatorStatusAsync(
        string userId,
        UpdateProviderOperatorStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await SendAsync<UpdateProviderOperatorStatusRequest, ProviderAccessOperatorResponse>(
            $"operators/{Uri.EscapeDataString(userId.Trim())}/status",
            request,
            "provider operator status",
            cancellationToken);

        return result.IsSuccess
            ? ControlCloudProviderOperatorClientResult.Success(result.Response!)
            : ControlCloudProviderOperatorClientResult.Failure(result.FailureCode!, result.Detail!);
    }

    private async Task<ProviderAccessHttpResult<TResponse>> GetAsync<TResponse>(
        string action,
        string actionLabel,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        var configurationFailure = ValidateConfiguration<TResponse>();

        if (configurationFailure is not null)
        {
            return configurationFailure;
        }

        try
        {
            var requestUri = BuildProviderAccessUri(action);
            using var message = new HttpRequestMessage(HttpMethod.Get, requestUri);
            AddProviderAccessHeader(message);
            using var response = await _httpClient.SendAsync(message, cancellationToken);

            return await ToResultAsync<TResponse>(response, actionLabel, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ProviderAccessHttpResult<TResponse>.Failure(
                "ControlCloudProviderAccessUnavailable",
                $"Timed out while reading Control Cloud {actionLabel}.");
        }
        catch (HttpRequestException exception)
        {
            return ProviderAccessHttpResult<TResponse>.Failure(
                "ControlCloudProviderAccessUnavailable",
                exception.Message);
        }
    }

    private async Task<ProviderAccessHttpResult<TResponse>> SendAsync<TRequest, TResponse>(
        string action,
        TRequest request,
        string actionLabel,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        var configurationFailure = ValidateConfiguration<TResponse>();

        if (configurationFailure is not null)
        {
            return configurationFailure;
        }

        try
        {
            var requestUri = BuildProviderAccessUri(action);
            using var message = new HttpRequestMessage(HttpMethod.Post, requestUri);
            AddProviderAccessHeader(message);
            message.Content = JsonContent.Create(request);
            using var response = await _httpClient.SendAsync(message, cancellationToken);

            return await ToResultAsync<TResponse>(response, actionLabel, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ProviderAccessHttpResult<TResponse>.Failure(
                "ControlCloudProviderAccessUnavailable",
                $"Timed out while updating Control Cloud {actionLabel}.");
        }
        catch (HttpRequestException exception)
        {
            return ProviderAccessHttpResult<TResponse>.Failure(
                "ControlCloudProviderAccessUnavailable",
                exception.Message);
        }
    }

    private async Task<ProviderAccessHttpResult<TResponse>> SendWithoutProviderAccessAsync<
        TRequest,
        TResponse>(
        string action,
        TRequest request,
        string actionLabel,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        var configurationFailure = ValidateBaseUrl<TResponse>();

        if (configurationFailure is not null)
        {
            return configurationFailure;
        }

        try
        {
            var requestUri = BuildProviderAccessUri(action);
            using var message = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = JsonContent.Create(request)
            };
            using var response = await _httpClient.SendAsync(message, cancellationToken);

            return await ToResultAsync<TResponse>(response, actionLabel, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ProviderAccessHttpResult<TResponse>.Failure(
                "ControlCloudProviderAccessUnavailable",
                $"Timed out while updating Control Cloud {actionLabel}.");
        }
        catch (HttpRequestException exception)
        {
            return ProviderAccessHttpResult<TResponse>.Failure(
                "ControlCloudProviderAccessUnavailable",
                exception.Message);
        }
    }

    private async Task<ProviderAccessHttpResult<TResponse>> ToResultAsync<TResponse>(
        HttpResponseMessage response,
        string actionLabel,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content
                .ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);

            return responseBody is null
                ? ProviderAccessHttpResult<TResponse>.Failure(
                    "ControlCloudProviderAccessResponseInvalid",
                    $"Control Cloud returned an empty {actionLabel} response.")
                : ProviderAccessHttpResult<TResponse>.Success(responseBody);
        }

        var error = await ReadErrorAsync(response, cancellationToken);

        return ProviderAccessHttpResult<TResponse>.Failure(
            error.Code ?? ToDefaultFailureCode(response.StatusCode),
            error.Detail);
    }

    private ProviderAccessHttpResult<TResponse>? ValidateConfiguration<TResponse>()
        where TResponse : class
    {
        var baseUrlFailure = ValidateBaseUrl<TResponse>();

        if (baseUrlFailure is not null)
        {
            return baseUrlFailure;
        }

        if (!HasProviderAccess())
        {
            return ProviderAccessHttpResult<TResponse>.Failure(
                "ControlCloudProviderAccessNotConfigured",
                "Control Cloud provider access token or secret is not configured.");
        }

        return null;
    }

    private ProviderAccessHttpResult<TResponse>? ValidateBaseUrl<TResponse>()
        where TResponse : class
    {
        if (!Uri.TryCreate(_options.Value.BaseUrl, UriKind.Absolute, out _))
        {
            return ProviderAccessHttpResult<TResponse>.Failure(
                "ControlCloudProviderAccessNotConfigured",
                "Control Cloud provider access base URL is not configured.");
        }

        return null;
    }

    private Uri BuildProviderAccessUri(string action)
    {
        var baseUri = new Uri(_options.Value.BaseUrl, UriKind.Absolute);

        return new Uri(baseUri, $"/api/v1/provider-access/{action}");
    }

    private void AddProviderAccessHeader(HttpRequestMessage message)
    {
        if (_credentialSource.TryGetCredential(
                _options.Value.ProviderAccessToken,
                _options.Value.ProviderAccessSecret,
                out var credential))
        {
            message.Headers.TryAddWithoutValidation(
                credential.HeaderName,
                credential.HeaderValue);
        }
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
            HttpStatusCode.NotFound => "ProviderOperatorNotFound",
            HttpStatusCode.Conflict => "ProviderOperatorAlreadyExists",
            HttpStatusCode.BadRequest => "ControlCloudProviderAccessInvalid",
            HttpStatusCode.Unauthorized => "ControlCloudProviderAccessDenied",
            HttpStatusCode.Forbidden => "ProviderAccessScopeDenied",
            HttpStatusCode.ServiceUnavailable => "ControlCloudProviderAccessNotConfigured",
            _ => "ControlCloudProviderAccessUnavailable"
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

    private sealed class ProviderAccessHttpResult<TResponse>
        where TResponse : class
    {
        private ProviderAccessHttpResult(
            TResponse? response,
            string? failureCode,
            string? detail)
        {
            Response = response;
            FailureCode = failureCode;
            Detail = detail;
        }

        public bool IsSuccess => Response is not null;

        public TResponse? Response { get; }

        public string? FailureCode { get; }

        public string? Detail { get; }

        public static ProviderAccessHttpResult<TResponse> Success(TResponse response)
        {
            return new ProviderAccessHttpResult<TResponse>(
                response,
                failureCode: null,
                detail: null);
        }

        public static ProviderAccessHttpResult<TResponse> Failure(
            string failureCode,
            string detail)
        {
            return new ProviderAccessHttpResult<TResponse>(
                response: null,
                failureCode,
                detail);
        }
    }
}
