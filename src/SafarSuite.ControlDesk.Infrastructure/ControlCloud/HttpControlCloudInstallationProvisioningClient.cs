using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Infrastructure.ControlCloud;

public sealed class HttpControlCloudInstallationProvisioningClient
    : IControlCloudInstallationProvisioningClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<ControlCloudStatusOptions> _options;
    private readonly IControlCloudProviderAccessCredentialSource _credentialSource;

    public HttpControlCloudInstallationProvisioningClient(
        HttpClient httpClient,
        IOptions<ControlCloudStatusOptions> options,
        IControlCloudProviderAccessCredentialSource credentialSource)
    {
        _httpClient = httpClient;
        _options = options;
        _credentialSource = credentialSource;
    }

    public Task<ControlCloudSetupTokenClientResult> CreateSetupTokenAsync(
        Guid clientId,
        string installationId,
        CreateLocalServerSetupTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        return CreateSetupTokenCoreAsync(clientId, installationId, request, cancellationToken);
    }

    public Task<ControlCloudBootstrapPackageClientResult> CreateBootstrapPackageAsync(
        Guid clientId,
        string installationId,
        CreateLocalServerBootstrapPackageRequest request,
        CancellationToken cancellationToken = default)
    {
        return CreateBootstrapPackageCoreAsync(clientId, installationId, request, cancellationToken);
    }

    public Task<ControlCloudBootstrapPackageRegisterClientResult> ListBootstrapPackagesAsync(
        Guid clientId,
        string installationId,
        int take,
        CancellationToken cancellationToken = default)
    {
        return ListBootstrapPackagesCoreAsync(clientId, installationId, take, cancellationToken);
    }

    public Task<ControlCloudAppActivationTokenClientResult> IssueAppActivationTokenAsync(
        Guid clientId,
        string installationId,
        IssueSafarSuiteAppActivationTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        return IssueAppActivationTokenCoreAsync(clientId, installationId, request, cancellationToken);
    }

    public Task<ControlCloudFirstManagerSetupTokenClientResult> IssueFirstManagerSetupTokenAsync(
        Guid clientId,
        string installationId,
        IssueLocalServerFirstManagerSetupTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        return IssueFirstManagerSetupTokenCoreAsync(clientId, installationId, request, cancellationToken);
    }

    public Task<ControlCloudAppActivationIssuesClientResult> ListAppActivationIssuesAsync(
        Guid clientId,
        string? installationId,
        Guid? appServerInstallationId,
        string? query,
        int take,
        CancellationToken cancellationToken = default)
    {
        return ListAppActivationIssuesCoreAsync(
            clientId,
            installationId,
            appServerInstallationId,
            query,
            take,
            cancellationToken);
    }

    public Task<ControlCloudAppActivationIssueClientResult> RevokeAppActivationIssueAsync(
        Guid clientId,
        Guid activationIssueId,
        RevokeSafarSuiteAppActivationIssueRequest request,
        CancellationToken cancellationToken = default)
    {
        return RevokeAppActivationIssueCoreAsync(
            clientId,
            activationIssueId,
            request,
            cancellationToken);
    }

    private async Task<ControlCloudSetupTokenClientResult> CreateSetupTokenCoreAsync(
        Guid clientId,
        string installationId,
        CreateLocalServerSetupTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await SendAsync<CreateLocalServerSetupTokenRequest, LocalServerSetupTokenResponse>(
            clientId,
            installationId,
            "setup-token",
            request,
            "setup token",
            cancellationToken);

        return result.IsSuccess
            ? ControlCloudSetupTokenClientResult.Success(result.Response!)
            : ControlCloudSetupTokenClientResult.Failure(result.FailureCode!, result.Detail!);
    }

    private async Task<ControlCloudBootstrapPackageClientResult> CreateBootstrapPackageCoreAsync(
        Guid clientId,
        string installationId,
        CreateLocalServerBootstrapPackageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await SendAsync<CreateLocalServerBootstrapPackageRequest, LocalServerBootstrapPackageResponse>(
            clientId,
            installationId,
            "bootstrap-package",
            request,
            "bootstrap package",
            cancellationToken);

        return result.IsSuccess
            ? ControlCloudBootstrapPackageClientResult.Success(result.Response!)
            : ControlCloudBootstrapPackageClientResult.Failure(result.FailureCode!, result.Detail!);
    }

    private async Task<ControlCloudBootstrapPackageRegisterClientResult> ListBootstrapPackagesCoreAsync(
        Guid clientId,
        string installationId,
        int take,
        CancellationToken cancellationToken)
    {
        if (!HasProviderAccess())
        {
            return ControlCloudBootstrapPackageRegisterClientResult.Failure(
                "ControlCloudProviderAccessNotConfigured",
                "Control Cloud provider access key is not configured.");
        }

        var result = await GetInstallationScopedAsync<LocalServerBootstrapPackageRegisterResponse>(
            clientId,
            installationId,
            "bootstrap-packages",
            new Dictionary<string, string?>
            {
                ["take"] = take.ToString()
            },
            "bootstrap package register",
            cancellationToken);

        return result.IsSuccess
            ? ControlCloudBootstrapPackageRegisterClientResult.Success(result.Response!)
            : ControlCloudBootstrapPackageRegisterClientResult.Failure(result.FailureCode!, result.Detail!);
    }

    private async Task<ControlCloudAppActivationTokenClientResult> IssueAppActivationTokenCoreAsync(
        Guid clientId,
        string installationId,
        IssueSafarSuiteAppActivationTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasProviderAccess())
        {
            return ControlCloudAppActivationTokenClientResult.Failure(
                "ControlCloudProviderAccessNotConfigured",
                "Control Cloud provider access key is not configured.");
        }

        var result = await SendAsync<IssueSafarSuiteAppActivationTokenRequest, IssueSafarSuiteAppActivationTokenResponse>(
            clientId,
            installationId,
            "app-activation-token",
            request,
            "app activation token",
            cancellationToken);

        return result.IsSuccess
            ? ControlCloudAppActivationTokenClientResult.Success(result.Response!)
            : ControlCloudAppActivationTokenClientResult.Failure(result.FailureCode!, result.Detail!);
    }

    private async Task<ControlCloudFirstManagerSetupTokenClientResult> IssueFirstManagerSetupTokenCoreAsync(
        Guid clientId,
        string installationId,
        IssueLocalServerFirstManagerSetupTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasProviderAccess())
        {
            return ControlCloudFirstManagerSetupTokenClientResult.Failure(
                "ControlCloudProviderAccessNotConfigured",
                "Control Cloud provider access key is not configured.");
        }

        var result = await SendAsync<IssueLocalServerFirstManagerSetupTokenRequest, IssueLocalServerFirstManagerSetupTokenResponse>(
            clientId,
            installationId,
            "first-manager-setup-token",
            request,
            "first-manager setup token",
            cancellationToken);

        return result.IsSuccess
            ? ControlCloudFirstManagerSetupTokenClientResult.Success(result.Response!)
            : ControlCloudFirstManagerSetupTokenClientResult.Failure(result.FailureCode!, result.Detail!);
    }

    private async Task<ControlCloudAppActivationIssuesClientResult> ListAppActivationIssuesCoreAsync(
        Guid clientId,
        string? installationId,
        Guid? appServerInstallationId,
        string? query,
        int take,
        CancellationToken cancellationToken)
    {
        if (!HasProviderAccess())
        {
            return ControlCloudAppActivationIssuesClientResult.Failure(
                "ControlCloudProviderAccessNotConfigured",
                "Control Cloud provider access key is not configured.");
        }

        var queryParameters = new Dictionary<string, string?>
        {
            ["installationId"] = string.IsNullOrWhiteSpace(installationId) ? null : installationId.Trim(),
            ["appServerInstallationId"] = appServerInstallationId?.ToString("D"),
            ["query"] = string.IsNullOrWhiteSpace(query) ? null : query.Trim(),
            ["take"] = take.ToString()
        };
        var result = await GetAsync<SafarSuiteAppActivationIssuesResponse>(
            clientId,
            "app-activation-issues",
            queryParameters,
            "app activation issues",
            cancellationToken);

        return result.IsSuccess
            ? ControlCloudAppActivationIssuesClientResult.Success(result.Response!)
            : ControlCloudAppActivationIssuesClientResult.Failure(result.FailureCode!, result.Detail!);
    }

    private async Task<ControlCloudAppActivationIssueClientResult> RevokeAppActivationIssueCoreAsync(
        Guid clientId,
        Guid activationIssueId,
        RevokeSafarSuiteAppActivationIssueRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasProviderAccess())
        {
            return ControlCloudAppActivationIssueClientResult.Failure(
                "ControlCloudProviderAccessNotConfigured",
                "Control Cloud provider access key is not configured.");
        }

        var result = await SendClientScopedAsync<RevokeSafarSuiteAppActivationIssueRequest, SafarSuiteAppActivationIssueResponse>(
            clientId,
            $"app-activation-issues/{activationIssueId:D}/revoke",
            request,
            "app activation issue revocation",
            cancellationToken);

        return result.IsSuccess
            ? ControlCloudAppActivationIssueClientResult.Success(result.Response!)
            : ControlCloudAppActivationIssueClientResult.Failure(result.FailureCode!, result.Detail!);
    }

    private async Task<ProvisioningHttpResult<TResponse>> SendAsync<TRequest, TResponse>(
        Guid clientId,
        string installationId,
        string action,
        TRequest request,
        string actionLabel,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        if (!Uri.TryCreate(_options.Value.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            return ProvisioningHttpResult<TResponse>.Failure(
                "ControlCloudInstallationProvisioningNotConfigured",
                "Control Cloud provisioning base URL is not configured.");
        }

        try
        {
            var requestUri = new Uri(
                baseUri,
                $"/api/v1/control-cloud/clients/{clientId:D}/installations/{Uri.EscapeDataString(installationId.Trim())}/{action}");
            using var message = new HttpRequestMessage(HttpMethod.Post, requestUri);
            AddProviderAccessHeader(message);
            message.Content = JsonContent.Create(request);
            using var response = await _httpClient.SendAsync(message, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content
                    .ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);

                return responseBody is null
                    ? ProvisioningHttpResult<TResponse>.Failure(
                        "ControlCloudInstallationProvisioningResponseInvalid",
                        $"Control Cloud returned an empty {actionLabel} response.")
                    : ProvisioningHttpResult<TResponse>.Success(responseBody);
            }

            var error = await ReadErrorAsync(response, cancellationToken);

            return ProvisioningHttpResult<TResponse>.Failure(
                error.Code ?? ToDefaultFailureCode(response.StatusCode),
                error.Detail);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ProvisioningHttpResult<TResponse>.Failure(
                "ControlCloudInstallationProvisioningUnavailable",
                $"Timed out while creating the Control Cloud {actionLabel}.");
        }
        catch (HttpRequestException exception)
        {
            return ProvisioningHttpResult<TResponse>.Failure(
                "ControlCloudInstallationProvisioningUnavailable",
                exception.Message);
        }
    }

    private async Task<ProvisioningHttpResult<TResponse>> SendClientScopedAsync<TRequest, TResponse>(
        Guid clientId,
        string action,
        TRequest request,
        string actionLabel,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        if (!Uri.TryCreate(_options.Value.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            return ProvisioningHttpResult<TResponse>.Failure(
                "ControlCloudInstallationProvisioningNotConfigured",
                "Control Cloud provisioning base URL is not configured.");
        }

        try
        {
            var requestUri = new Uri(
                baseUri,
                $"/api/v1/control-cloud/clients/{clientId:D}/{action}");
            using var message = new HttpRequestMessage(HttpMethod.Post, requestUri);
            AddProviderAccessHeader(message);
            message.Content = JsonContent.Create(request);
            using var response = await _httpClient.SendAsync(message, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content
                    .ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);

                return responseBody is null
                    ? ProvisioningHttpResult<TResponse>.Failure(
                        "ControlCloudInstallationProvisioningResponseInvalid",
                        $"Control Cloud returned an empty {actionLabel} response.")
                    : ProvisioningHttpResult<TResponse>.Success(responseBody);
            }

            var error = await ReadErrorAsync(response, cancellationToken);

            return ProvisioningHttpResult<TResponse>.Failure(
                error.Code ?? ToDefaultFailureCode(response.StatusCode),
                error.Detail);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ProvisioningHttpResult<TResponse>.Failure(
                "ControlCloudInstallationProvisioningUnavailable",
                $"Timed out while creating the Control Cloud {actionLabel}.");
        }
        catch (HttpRequestException exception)
        {
            return ProvisioningHttpResult<TResponse>.Failure(
                "ControlCloudInstallationProvisioningUnavailable",
                exception.Message);
        }
    }

    private async Task<ProvisioningHttpResult<TResponse>> GetAsync<TResponse>(
        Guid clientId,
        string action,
        IReadOnlyDictionary<string, string?> queryParameters,
        string actionLabel,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        if (!Uri.TryCreate(_options.Value.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            return ProvisioningHttpResult<TResponse>.Failure(
                "ControlCloudInstallationProvisioningNotConfigured",
                "Control Cloud provisioning base URL is not configured.");
        }

        try
        {
            var query = BuildQueryString(queryParameters);
            var requestUri = new Uri(
                baseUri,
                $"/api/v1/control-cloud/clients/{clientId:D}/{action}{query}");
            using var message = new HttpRequestMessage(HttpMethod.Get, requestUri);
            AddProviderAccessHeader(message);
            using var response = await _httpClient.SendAsync(message, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content
                    .ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);

                return responseBody is null
                    ? ProvisioningHttpResult<TResponse>.Failure(
                        "ControlCloudInstallationProvisioningResponseInvalid",
                        $"Control Cloud returned an empty {actionLabel} response.")
                    : ProvisioningHttpResult<TResponse>.Success(responseBody);
            }

            var error = await ReadErrorAsync(response, cancellationToken);

            return ProvisioningHttpResult<TResponse>.Failure(
                error.Code ?? ToDefaultFailureCode(response.StatusCode),
                error.Detail);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ProvisioningHttpResult<TResponse>.Failure(
                "ControlCloudInstallationProvisioningUnavailable",
                $"Timed out while reading the Control Cloud {actionLabel}.");
        }
        catch (HttpRequestException exception)
        {
            return ProvisioningHttpResult<TResponse>.Failure(
                "ControlCloudInstallationProvisioningUnavailable",
                exception.Message);
        }
    }

    private async Task<ProvisioningHttpResult<TResponse>> GetInstallationScopedAsync<TResponse>(
        Guid clientId,
        string installationId,
        string action,
        IReadOnlyDictionary<string, string?> queryParameters,
        string actionLabel,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        if (!Uri.TryCreate(_options.Value.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            return ProvisioningHttpResult<TResponse>.Failure(
                "ControlCloudInstallationProvisioningNotConfigured",
                "Control Cloud provisioning base URL is not configured.");
        }

        try
        {
            var query = BuildQueryString(queryParameters);
            var requestUri = new Uri(
                baseUri,
                $"/api/v1/control-cloud/clients/{clientId:D}/installations/{Uri.EscapeDataString(installationId.Trim())}/{action}{query}");
            using var message = new HttpRequestMessage(HttpMethod.Get, requestUri);
            AddProviderAccessHeader(message);
            using var response = await _httpClient.SendAsync(message, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content
                    .ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);

                return responseBody is null
                    ? ProvisioningHttpResult<TResponse>.Failure(
                        "ControlCloudInstallationProvisioningResponseInvalid",
                        $"Control Cloud returned an empty {actionLabel} response.")
                    : ProvisioningHttpResult<TResponse>.Success(responseBody);
            }

            var error = await ReadErrorAsync(response, cancellationToken);

            return ProvisioningHttpResult<TResponse>.Failure(
                error.Code ?? ToDefaultFailureCode(response.StatusCode),
                error.Detail);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ProvisioningHttpResult<TResponse>.Failure(
                "ControlCloudInstallationProvisioningUnavailable",
                $"Timed out while reading the Control Cloud {actionLabel}.");
        }
        catch (HttpRequestException exception)
        {
            return ProvisioningHttpResult<TResponse>.Failure(
                "ControlCloudInstallationProvisioningUnavailable",
                exception.Message);
        }
    }

    private static string BuildQueryString(
        IReadOnlyDictionary<string, string?> queryParameters)
    {
        var pairs = queryParameters
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair =>
                $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value!.Trim())}")
            .ToArray();

        return pairs.Length == 0 ? "" : $"?{string.Join("&", pairs)}";
    }

    private static string ToDefaultFailureCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.NotFound => "ClientNotFound",
            HttpStatusCode.Conflict => "InstallationClientMismatch",
            HttpStatusCode.BadRequest => "ControlCloudInstallationProvisioningInvalid",
            HttpStatusCode.Unauthorized => "ControlCloudProviderAccessDenied",
            HttpStatusCode.ServiceUnavailable => "ControlCloudProviderAccessNotConfigured",
            _ => "ControlCloudInstallationProvisioningUnavailable"
        };
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

    private sealed class ProvisioningHttpResult<TResponse>
        where TResponse : class
    {
        private ProvisioningHttpResult(
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

        public static ProvisioningHttpResult<TResponse> Success(TResponse response)
        {
            return new ProvisioningHttpResult<TResponse>(
                response,
                failureCode: null,
                detail: null);
        }

        public static ProvisioningHttpResult<TResponse> Failure(
            string failureCode,
            string detail)
        {
            return new ProvisioningHttpResult<TResponse>(
                response: null,
                failureCode,
                detail);
        }
    }
}
