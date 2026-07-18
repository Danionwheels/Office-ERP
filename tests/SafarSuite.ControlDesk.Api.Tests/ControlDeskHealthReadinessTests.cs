using System.Net;
using System.Net.Http.Json;
using SafarSuite.ControlDesk.Application.Modules.Diagnostics.Ports;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Health;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class ControlDeskHealthReadinessTests(ControlDeskApiFactory factory)
    : IClassFixture<ControlDeskApiFactory>
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/live")]
    public async Task Liveness_stays_anonymous_and_healthy_during_database_failure(
        string route)
    {
        var database = new MutableOfficeDatabaseReadinessProbe();
        database.SetUnavailable();
        var controlCloud = new MutableControlCloudReachabilityProbe();
        await using var host = ControlDeskProbeTestHost.Create(factory, database, controlCloud);
        using var client = host.CreateClient();

        using var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertNoStore(response);
        var body = await response.Content.ReadAsStringAsync();
        AssertSanitized(body);
    }

    [Theory]
    [InlineData("/ready")]
    [InlineData("/health/ready")]
    public async Task Readiness_returns_sanitized_503_then_200_after_same_probe_recovers(
        string route)
    {
        var database = new MutableOfficeDatabaseReadinessProbe();
        database.SetUnavailable();
        var controlCloud = new MutableControlCloudReachabilityProbe();
        await using var host = ControlDeskProbeTestHost.Create(factory, database, controlCloud);
        using var client = host.CreateClient();

        using var unavailableResponse = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailableResponse.StatusCode);
        AssertNoStore(unavailableResponse);
        var unavailableBody = await unavailableResponse.Content.ReadAsStringAsync();
        AssertSanitized(unavailableBody);
        var unavailable = await unavailableResponse.Content.ReadFromJsonAsync<ReadinessResponse>();
        Assert.NotNull(unavailable);
        Assert.Equal("NotReady", unavailable.Status);
        Assert.Equal(OfficeDatabaseReadinessCodes.DatabaseUnavailable, unavailable.Code);
        Assert.Equal("Unavailable", unavailable.Database.ConnectivityStatus);

        database.SetReady();

        using var readyResponse = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);
        AssertNoStore(readyResponse);
        var readyBody = await readyResponse.Content.ReadAsStringAsync();
        AssertSanitized(readyBody);
        var ready = await readyResponse.Content.ReadFromJsonAsync<ReadinessResponse>();
        Assert.NotNull(ready);
        Assert.Equal("Ready", ready.Status);
        Assert.Equal(OfficeDatabaseReadinessCodes.Ready, ready.Code);
        Assert.Equal("Current", ready.Database.MigrationStatus);
    }

    private static void AssertNoStore(HttpResponseMessage response)
    {
        Assert.True(
            response.Headers.CacheControl?.NoStore == true,
            $"Expected Cache-Control: no-store, received '{response.Headers.CacheControl}'.");
    }

    private static void AssertSanitized(string body)
    {
        AssertDoesNotContain(body, MutableOfficeDatabaseReadinessProbe.SentinelConnectionString);
        AssertDoesNotContain(body, "sentinel-password");
        AssertDoesNotContain(body, MutableOfficeDatabaseReadinessProbe.SentinelFailureDetail);
        AssertDoesNotContain(body, MutableControlCloudReachabilityProbe.SentinelSecretPayload);
        AssertDoesNotContain(body, MutableControlCloudReachabilityProbe.SentinelFailureDetail);
    }

    private static void AssertDoesNotContain(string body, string sentinel)
    {
        Assert.False(
            body.Contains(sentinel, StringComparison.OrdinalIgnoreCase),
            $"Response exposed sentinel content '{sentinel}'.");
    }
}
