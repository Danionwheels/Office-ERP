using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SafarSuite.ControlDesk.Application.Modules.Diagnostics.Ports;

namespace SafarSuite.ControlDesk.Api.Tests;

internal sealed class MutableOfficeDatabaseReadinessProbe : IOfficeDatabaseReadinessProbe
{
    public const string SentinelConnectionString =
        "Host=sentinel-office-db;Username=sentinel-user;Password=sentinel-password";

    public const string SentinelFailureDetail =
        "sentinel-database-failure-detail-that-must-never-be-returned";

    private readonly object _sync = new();
    private OfficeDatabaseReadinessResult _current = UnavailableResult();

    public Task<OfficeDatabaseReadinessResult> CheckAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            return Task.FromResult(_current);
        }
    }

    public void SetUnavailable()
    {
        lock (_sync)
        {
            _current = UnavailableResult();
        }
    }

    public void SetReady()
    {
        lock (_sync)
        {
            _current = new OfficeDatabaseReadinessResult(
                true,
                OfficeDatabaseReadinessCodes.Ready,
                "Postgres",
                OfficeDatabaseConnectivityStatus.Ready,
                OfficeDatabaseMigrationStatus.Current,
                32,
                32,
                0,
                0);
        }
    }

    private static OfficeDatabaseReadinessResult UnavailableResult() =>
        new(
            false,
            OfficeDatabaseReadinessCodes.DatabaseUnavailable,
            "Postgres",
            OfficeDatabaseConnectivityStatus.Unavailable,
            OfficeDatabaseMigrationStatus.Indeterminate,
            null,
            null,
            null,
            null);
}

internal sealed class MutableControlCloudReachabilityProbe : IControlCloudReachabilityProbe
{
    public const string SentinelSecretPayload =
        "sentinel-cloud-token-and-payload-that-must-never-be-returned";

    public const string SentinelFailureDetail =
        "sentinel-cloud-failure-detail-that-must-never-be-returned";

    private readonly object _sync = new();
    private ControlCloudReachabilityResult _current = UnreachableResult();

    public Task<ControlCloudReachabilityResult> CheckAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            return Task.FromResult(_current);
        }
    }

    public void SetReachable()
    {
        lock (_sync)
        {
            _current = new ControlCloudReachabilityResult(
                ControlCloudReachabilityStatus.Reachable,
                ControlCloudReachabilityCodes.Reachable,
                200,
                12);
        }
    }

    public void SetUnreachable()
    {
        lock (_sync)
        {
            _current = UnreachableResult();
        }
    }

    private static ControlCloudReachabilityResult UnreachableResult() =>
        new(
            ControlCloudReachabilityStatus.Unreachable,
            ControlCloudReachabilityCodes.Unreachable,
            null,
            null);
}

internal static class ControlDeskProbeTestHost
{
    public static WebApplicationFactory<Program> Create(
        ControlDeskApiFactory factory,
        MutableOfficeDatabaseReadinessProbe database,
        MutableControlCloudReachabilityProbe controlCloud) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IOfficeDatabaseReadinessProbe>();
                services.RemoveAll<IControlCloudReachabilityProbe>();
                services.AddSingleton<IOfficeDatabaseReadinessProbe>(database);
                services.AddSingleton<IControlCloudReachabilityProbe>(controlCloud);
            });
        });
}
