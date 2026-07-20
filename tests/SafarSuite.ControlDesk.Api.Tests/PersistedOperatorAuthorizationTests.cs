using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SafarSuite.ControlDesk.Application.Modules.Auth.Ports;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Auth;
using SafarSuite.ControlDesk.Domain.Modules.Auth;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class PersistedOperatorAuthorizationTests
{
    [Fact]
    public async Task Login_and_authorization_use_repository_operator_not_configured_users()
    {
        await using var factory = new PersistedOperatorControlDeskApiFactory();
        using var client = factory.CreateClient();

        var session = await SignInAsync(
            client,
            PersistedOperatorControlDeskApiFactory.PersistedEmail);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            session.AccessToken);

        var response = await client.GetAsync("/api/v1/contracts/product-modules");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(LocalOperatorRole.Administrator, session.Roles);
        Assert.Contains(LocalOperatorScope.Admin, session.Scopes);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("status")]
    [InlineData("permissions")]
    public async Task Protected_change_immediately_invalidates_existing_session(string change)
    {
        await using var factory = new PersistedOperatorControlDeskApiFactory();
        using var client = factory.CreateClient();
        var session = await SignInAsync(
            client,
            PersistedOperatorControlDeskApiFactory.PersistedEmail);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            session.AccessToken);

        using var scope = factory.Services.CreateScope();
        var operators = scope.ServiceProvider.GetRequiredService<ILocalOperatorRepository>();
        var localOperator = await operators.GetByNormalizedEmailAsync(
            PersistedOperatorControlDeskApiFactory.PersistedEmail.ToUpperInvariant())
            ?? throw new InvalidOperationException("The persisted test operator was not found.");
        var changedAtUtc = new DateTimeOffset(2026, 7, 20, 1, 0, 0, TimeSpan.Zero);

        switch (change)
        {
            case "password":
                localOperator.ChangePasswordHash("changed-password-hash", changedAtUtc);
                break;
            case "status":
                localOperator.Disable(changedAtUtc);
                break;
            case "permissions":
                localOperator.ChangeAccess(
                    [LocalOperatorRole.Administrator, LocalOperatorRole.Auditor],
                    [LocalOperatorScope.Admin, LocalOperatorScope.ReportsRead],
                    changedAtUtc);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(change), change, null);
        }

        var response = await client.GetAsync("/api/v1/contracts/product-modules");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<LocalOperatorSessionResponse> SignInAsync(
        HttpClient client,
        string email)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/operator-sessions",
            new CreateLocalOperatorSessionRequest(email, ControlDeskApiFactory.Password, 5));
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<LocalOperatorSessionResponse>()
               ?? throw new InvalidOperationException("The sign-in response was empty.");
    }
}
