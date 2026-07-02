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

    public HttpControlCloudInstallationProvisioningClient(
        HttpClient httpClient,
        IOptions<ControlCloudStatusOptions> options)
    {
        _httpClient = httpClient;
        _options = options;
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
            using var response = await _httpClient.PostAsJsonAsync(
                requestUri,
                request,
                cancellationToken);

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

    private static string ToDefaultFailureCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.NotFound => "ClientNotFound",
            HttpStatusCode.Conflict => "InstallationClientMismatch",
            HttpStatusCode.BadRequest => "ControlCloudInstallationProvisioningInvalid",
            _ => "ControlCloudInstallationProvisioningUnavailable"
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
