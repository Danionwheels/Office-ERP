using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SafarSuite.ControlDesk.Api.Composition;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class ControlDeskRetainedLoggingTests
{
    [Fact]
    public async Task Retained_log_survives_controlled_host_stop_without_recording_secrets()
    {
        const string sentinelSigningSecret = "LOG-SENTINEL-SIGNING-SECRET-DO-NOT-RETAIN";
        const string sentinelBearerToken = "LOG-SENTINEL-BEARER-TOKEN-DO-NOT-RETAIN";
        var logDirectory = Path.Combine(
            Path.GetTempPath(),
            "safarsuite-control-desk-log-tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = "Testing"
            });
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ControlDesk:Logging:File:Enabled"] = "true",
                ["ControlDesk:Logging:File:DirectoryPath"] = logDirectory,
                ["ControlDesk:Logging:File:RetainedFileCountLimit"] = "4",
                ["ControlDesk:Logging:File:RetainedDays"] = "2",
                ["ControlDesk:OperatorAccess:SessionSigningSecret"] = sentinelSigningSecret,
                ["Authorization:Sentinel"] = sentinelBearerToken
            });
            builder.Logging.ClearProviders();
            builder.AddControlDeskRetainedFileLogging();

            await using (var app = builder.Build())
            {
                var logger = app.Services
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("SafarSuite.ControlDesk.Api.Tests.Lifecycle");
                app.Lifetime.ApplicationStarted.Register(() =>
                    logger.LogInformation("EventCode={EventCode}", "OfficeHostStarted"));
                app.Lifetime.ApplicationStopping.Register(() =>
                    logger.LogInformation("EventCode={EventCode}", "OfficeHostStopping"));
                app.Lifetime.ApplicationStopped.Register(() =>
                    logger.LogInformation("EventCode={EventCode}", "OfficeHostStopped"));

                await app.StartAsync();
                await app.StopAsync();
            }

            var logFiles = Directory.GetFiles(logDirectory, "control-desk-*.jsonl");
            Assert.NotEmpty(logFiles);
            var retainedLog = string.Join(
                Environment.NewLine,
                logFiles.Select(File.ReadAllText));

            Assert.Contains("OfficeHostStarted", retainedLog);
            Assert.Contains("OfficeHostStopping", retainedLog);
            Assert.Contains("OfficeHostStopped", retainedLog);
            Assert.DoesNotContain(sentinelSigningSecret, retainedLog);
            Assert.DoesNotContain(sentinelBearerToken, retainedLog);
        }
        finally
        {
            if (Directory.Exists(logDirectory))
            {
                Directory.Delete(logDirectory, recursive: true);
            }
        }
    }
}
