using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SafarSuite.ConnectedAcceptance;

internal sealed class AcceptanceHttpClient : IDisposable
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client;

    public AcceptanceHttpClient(Uri baseAddress)
    {
        _client = new HttpClient
        {
            BaseAddress = baseAddress,
            Timeout = TimeSpan.FromMinutes(2)
        };
    }

    public void UseBearerToken(string accessToken)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(HttpMethod.Get, path, body: null, cancellationToken);
    }

    public Task<JsonElement> GetElementAsync(string path, CancellationToken cancellationToken = default)
    {
        return SendElementAsync(HttpMethod.Get, path, body: null, cancellationToken);
    }

    public Task<T> PostAsync<T>(
        string path,
        object? body = null,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(HttpMethod.Post, path, body, cancellationToken);
    }

    public Task<JsonElement> PostElementAsync(
        string path,
        object? body = null,
        CancellationToken cancellationToken = default)
    {
        return SendElementAsync(HttpMethod.Post, path, body, cancellationToken);
    }

    public Task<T> PutAsync<T>(
        string path,
        object body,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(HttpMethod.Put, path, body, cancellationToken);
    }

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        var json = await SendForJsonAsync(method, path, body, cancellationToken);

        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new ConnectedAcceptanceFailureException(
                $"{method} {path} returned an empty or incompatible response.");
    }

    private async Task<JsonElement> SendElementAsync(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        var json = await SendForJsonAsync(method, path, body, cancellationToken);
        using var document = JsonDocument.Parse(json);

        return document.RootElement.Clone();
    }

    private async Task<string> SendForJsonAsync(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);

        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body, body.GetType(), JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var response = await _client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new ConnectedAcceptanceFailureException(
                $"{method} {path} returned {(int)response.StatusCode} {response.ReasonPhrase}: {responseBody}");
        }

        if (string.IsNullOrWhiteSpace(responseBody))
        {
            throw new ConnectedAcceptanceFailureException(
                $"{method} {path} returned no response body.");
        }

        return responseBody;
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
