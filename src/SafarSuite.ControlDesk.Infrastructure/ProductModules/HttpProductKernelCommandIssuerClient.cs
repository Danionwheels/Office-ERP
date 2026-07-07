using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Contracts.SafarSuiteApp.V1;

namespace SafarSuite.ControlDesk.Infrastructure.ProductModules;

public sealed class ProductKernelCommandIssuerOptions
{
    public const string SectionName = "SafarSuiteApp:ProductKernelCommands";

    public string BaseUrl { get; set; } = string.Empty;

    public string OwnerApiKey { get; set; } = string.Empty;

    public string OwnerActor { get; set; } = "SafarSuite.ControlDesk";
}

public sealed class HttpProductKernelCommandIssuerClient
    : IProductKernelCommandIssuerClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<ProductKernelCommandIssuerOptions> _options;

    public HttpProductKernelCommandIssuerClient(
        HttpClient httpClient,
        IOptions<ProductKernelCommandIssuerOptions> options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<ProductKernelCommandIssueClientResult> IssueCommandAsync(
        Guid activationRequestId,
        IssueProductKernelCommandRequest request,
        string requestedBy,
        CancellationToken cancellationToken = default)
    {
        var options = _options.Value;
        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            return ProductKernelCommandIssueClientResult.Failure(
                "ProductKernelCommandIssuerNotConfigured",
                "SafarSuite app product-kernel command issuer base URL is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.OwnerApiKey))
        {
            return ProductKernelCommandIssueClientResult.Failure(
                "ProductKernelCommandIssuerNotConfigured",
                "SafarSuite app owner API key is not configured.");
        }

        try
        {
            var requestUri = new Uri(
                baseUri,
                $"/api/activation/server-requests/{activationRequestId:D}/product-kernel-commands");
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = JsonContent.Create(request)
            };

            httpRequest.Headers.TryAddWithoutValidation("X-Owner-Api-Key", options.OwnerApiKey.Trim());
            httpRequest.Headers.TryAddWithoutValidation(
                "X-Owner-Actor",
                string.IsNullOrWhiteSpace(requestedBy) ? options.OwnerActor : requestedBy.Trim());

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var command = await response.Content.ReadFromJsonAsync<CloudProductKernelCommandResponse>(
                    cancellationToken: cancellationToken);

                return command is null
                    ? ProductKernelCommandIssueClientResult.Failure(
                        "ProductKernelCommandIssuerResponseInvalid",
                        "SafarSuite app cloud server returned an empty product-kernel command response.")
                    : ProductKernelCommandIssueClientResult.Success(command);
            }

            var error = await ReadErrorAsync(response, cancellationToken);
            return ProductKernelCommandIssueClientResult.Failure(
                error.Code ?? ToDefaultFailureCode(response.StatusCode),
                error.Detail);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ProductKernelCommandIssueClientResult.Failure(
                "ProductKernelCommandIssuerUnavailable",
                "Timed out while issuing the SafarSuite app product-kernel command.");
        }
        catch (HttpRequestException exception)
        {
            return ProductKernelCommandIssueClientResult.Failure(
                "ProductKernelCommandIssuerUnavailable",
                exception.Message);
        }
    }

    private static string ToDefaultFailureCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => "ProductKernelCommandRejected",
            HttpStatusCode.Unauthorized => "ProductKernelCommandUnauthorized",
            HttpStatusCode.Forbidden => "ProductKernelCommandForbidden",
            HttpStatusCode.NotFound => "ActivationRequestNotFound",
            _ => "ProductKernelCommandIssuerUnavailable"
        };
    }

    private static async Task<IssuerError> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var validation = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(
                cancellationToken: cancellationToken);
            if (validation?.Errors is { Count: > 0 })
            {
                return new IssuerError(null, string.Join(" ", validation.Errors));
            }
        }
        catch (InvalidOperationException)
        {
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var detail = string.IsNullOrWhiteSpace(body)
            ? $"SafarSuite app cloud server returned HTTP {(int)response.StatusCode}."
            : body;

        return new IssuerError(null, detail);
    }

    private sealed record IssuerError(string? Code, string Detail);

    private sealed record ValidationErrorResponse(IReadOnlyCollection<string>? Errors);
}
