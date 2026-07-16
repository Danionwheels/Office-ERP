using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Auth;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class ControlDeskAuthorizationTests(ControlDeskApiFactory factory)
    : IClassFixture<ControlDeskApiFactory>
{
    [Fact]
    public async Task Health_remains_available_without_authentication()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected a successful authorized response, got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    [Fact]
    public async Task Every_business_endpoint_declares_a_scoped_authorization_policy()
    {
        using var client = factory.CreateClient();
        await client.GetAsync("/health");
        var endpointDataSource = factory.Services.GetRequiredService<EndpointDataSource>();
        var unprotectedRoutes = endpointDataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api/v1/", StringComparison.Ordinal) == true)
            .Where(endpoint => endpoint.RoutePattern.RawText != "/api/v1/auth/operator-sessions")
            .Where(endpoint => endpoint.Metadata
                .GetOrderedMetadata<IAuthorizeData>()
                .All(metadata => string.IsNullOrWhiteSpace(metadata.Policy)))
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .OrderBy(route => route, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(unprotectedRoutes);
    }

    [Fact]
    public async Task Business_endpoint_returns_401_for_anonymous_request()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/contracts/product-modules");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Business_endpoint_returns_403_when_scope_is_missing()
    {
        using var client = factory.CreateClient();
        var session = await SignInAsync(client, ControlDeskApiFactory.ReportReaderEmail);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

        var response = await client.GetAsync("/api/v1/contracts/product-modules");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Matching_module_scope_can_access_protected_endpoint()
    {
        using var client = factory.CreateClient();
        var session = await SignInAsync(client, ControlDeskApiFactory.ReportReaderEmail);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

        var response = await client.GetAsync(
            "/api/v1/accounting/revenue-summary?fromDate=2026-01-01&toDate=2026-01-31&currencyCode=PKR");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Auditor", session.Roles);
        Assert.Contains("reports:read", session.Scopes);
    }

    [Fact]
    public async Task Admin_scope_can_access_protected_business_endpoint()
    {
        using var client = factory.CreateClient();
        var session = await SignInAsync(client, ControlDeskApiFactory.AdminEmail);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

        var response = await client.GetAsync("/api/v1/contracts/product-modules");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected a successful authorized response, got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        Assert.Equal("Bearer", session.TokenType);
        Assert.Contains("Administrator", session.Roles);
        Assert.Contains("control-desk:admin", session.Scopes);
    }

    [Fact]
    public async Task Tampered_session_token_returns_401()
    {
        using var client = factory.CreateClient();
        var session = await SignInAsync(client, ControlDeskApiFactory.AdminEmail);
        var finalCharacter = session.AccessToken[^1] == 'A' ? 'B' : 'A';
        var tamperedToken = $"{session.AccessToken[..^1]}{finalCharacter}";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);

        var response = await client.GetAsync("/api/v1/contracts/product-modules");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Expired_session_token_returns_401()
    {
        await using var isolatedFactory = new ControlDeskApiFactory();
        using var client = isolatedFactory.CreateClient();
        var session = await SignInAsync(client, ControlDeskApiFactory.AdminEmail);
        isolatedFactory.Clock.Advance(TimeSpan.FromMinutes(6));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

        var response = await client.GetAsync("/api/v1/contracts/product-modules");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<LocalOperatorSessionResponse> SignInAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/operator-sessions",
            new CreateLocalOperatorSessionRequest(email, ControlDeskApiFactory.Password, 5));
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<LocalOperatorSessionResponse>()
               ?? throw new InvalidOperationException("The sign-in response was empty.");
    }
}
