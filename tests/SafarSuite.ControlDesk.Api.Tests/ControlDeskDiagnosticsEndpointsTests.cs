using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Application.Modules.Diagnostics.Ports;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Auth;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Diagnostics;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class ControlDeskDiagnosticsEndpointsTests(ControlDeskApiFactory factory)
    : IClassFixture<ControlDeskApiFactory>
{
    private const string SummaryRoute = "/api/v1/diagnostics/summary";

    [Fact]
    public async Task Diagnostics_returns_401_for_anonymous_request()
    {
        var database = ReadyDatabase();
        var controlCloud = ReachableControlCloud();
        await using var host = ControlDeskProbeTestHost.Create(factory, database, controlCloud);
        using var client = host.CreateClient();

        using var response = await client.GetAsync(SummaryRoute);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Diagnostics_returns_403_for_report_reader_without_diagnostics_scope()
    {
        var database = ReadyDatabase();
        var controlCloud = ReachableControlCloud();
        await using var host = ControlDeskProbeTestHost.Create(factory, database, controlCloud);
        using var client = host.CreateClient();
        await SignInAsync(client, ControlDeskApiFactory.ReportReaderEmail);

        using var response = await client.GetAsync(SummaryRoute);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Diagnostics_returns_sanitized_summary_for_admin()
    {
        var database = ReadyDatabase();
        var controlCloud = ReachableControlCloud();
        await using var host = ControlDeskProbeTestHost.Create(factory, database, controlCloud);
        using var client = host.CreateClient();
        await SignInAsync(client, ControlDeskApiFactory.AdminEmail);

        using var response = await client.GetAsync(SummaryRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertNoStore(response);
        var body = await response.Content.ReadAsStringAsync();
        AssertSanitized(body);
        var summary = await response.Content.ReadFromJsonAsync<OfficeDiagnosticsResponse>();
        Assert.NotNull(summary);
        Assert.Equal("Healthy", summary.Status);
        Assert.Equal("Ready", summary.Database.Status);
        Assert.Equal(OfficeDatabaseReadinessCodes.Ready, summary.Database.Code);
        Assert.Equal("Reachable", summary.ControlCloud.Status);
        Assert.Equal(ControlCloudReachabilityCodes.Reachable, summary.ControlCloud.Code);
    }

    [Fact]
    public async Task Diagnostics_scope_can_read_summary_without_admin_scope()
    {
        var database = ReadyDatabase();
        var controlCloud = ReachableControlCloud();
        await using var host = ControlDeskProbeTestHost.Create(factory, database, controlCloud);
        using var client = host.CreateClient();
        await SignInAsync(client, ControlDeskApiFactory.DiagnosticsReaderEmail);

        using var response = await client.GetAsync(SummaryRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<OfficeDiagnosticsResponse>();
        Assert.NotNull(summary);
        Assert.Equal("Healthy", summary.Status);
    }

    [Fact]
    public async Task Diagnostics_returns_partial_sanitized_summary_when_database_is_unavailable()
    {
        var database = new MutableOfficeDatabaseReadinessProbe();
        database.SetUnavailable();
        var controlCloud = ReachableControlCloud();
        await using var host = ControlDeskProbeTestHost.Create(factory, database, controlCloud);
        using var client = host.CreateClient();
        await SignInAsync(client, ControlDeskApiFactory.AdminEmail);

        using var response = await client.GetAsync(SummaryRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertNoStore(response);
        var body = await response.Content.ReadAsStringAsync();
        AssertSanitized(body);
        var summary = await response.Content.ReadFromJsonAsync<OfficeDiagnosticsResponse>();
        Assert.NotNull(summary);
        Assert.Equal("Unavailable", summary.Status);
        Assert.Equal("NotReady", summary.Database.Status);
        Assert.Equal(OfficeDatabaseReadinessCodes.DatabaseUnavailable, summary.Database.Code);
        Assert.Equal("Unavailable", summary.Outbox.Status);
        Assert.Null(summary.Database.KnownMigrationCount);
        Assert.Null(summary.Database.AppliedMigrationCount);
        Assert.Null(summary.Database.PendingMigrationCount);
        Assert.Null(summary.Database.UnknownAppliedMigrationCount);
        Assert.Null(summary.Outbox.TotalCount);
        Assert.Null(summary.Outbox.PendingCount);
        Assert.Equal("Reachable", summary.ControlCloud.Status);
    }

    [Fact]
    public async Task Diagnostics_degrades_when_ready_outbox_work_has_no_automatic_publisher()
    {
        var database = ReadyDatabase();
        var controlCloud = ReachableControlCloud();
        await using var host = ControlDeskProbeTestHost.Create(factory, database, controlCloud);
        var outbox = host.Services.GetRequiredService<ICloudOutboxMessageRepository>();
        await outbox.AddAsync(CloudOutboxMessage.Create(
            CloudOutboxMessageId.Create(Guid.NewGuid()),
            ClientId.Create(Guid.NewGuid()),
            "TestMessage",
            "TestSubject",
            Guid.NewGuid().ToString("D"),
            "{\"value\":1}",
            DateTimeOffset.UtcNow));
        using var client = host.CreateClient();
        await SignInAsync(client, ControlDeskApiFactory.AdminEmail);

        using var response = await client.GetAsync(SummaryRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<OfficeDiagnosticsResponse>();
        Assert.NotNull(summary);
        Assert.Equal("Degraded", summary.Status);
        Assert.Equal("AttentionRequired", summary.Outbox.Status);
        Assert.Equal(1, summary.Outbox.ReadyForPublishingCount);
        Assert.False(summary.Outbox.Automation.Enabled);
    }

    private static MutableOfficeDatabaseReadinessProbe ReadyDatabase()
    {
        var database = new MutableOfficeDatabaseReadinessProbe();
        database.SetReady();
        return database;
    }

    private static MutableControlCloudReachabilityProbe ReachableControlCloud()
    {
        var controlCloud = new MutableControlCloudReachabilityProbe();
        controlCloud.SetReachable();
        return controlCloud;
    }

    private static async Task SignInAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/operator-sessions",
            new CreateLocalOperatorSessionRequest(
                email,
                ControlDeskApiFactory.Password,
                5));
        response.EnsureSuccessStatusCode();
        var session = await response.Content.ReadFromJsonAsync<LocalOperatorSessionResponse>()
                      ?? throw new InvalidOperationException("The sign-in response was empty.");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            session.TokenType,
            session.AccessToken);
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
