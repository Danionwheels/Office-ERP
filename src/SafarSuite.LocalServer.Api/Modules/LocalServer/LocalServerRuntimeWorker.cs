using SafarSuite.LocalServer.Application.Commands.ProcessInstallationCommandsFromBootstrapConfiguration;
using SafarSuite.LocalServer.Application.Entitlements.PullEntitlementFromBootstrapConfiguration;
using SafarSuite.LocalServer.Application.Heartbeats.ReportHeartbeatFromBootstrapConfiguration;

namespace SafarSuite.LocalServer.Api.Modules.LocalServer;

public sealed class LocalServerRuntimeWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly LocalServerRuntimeAutomationOptions _options;
    private readonly ILogger<LocalServerRuntimeWorker> _logger;

    public LocalServerRuntimeWorker(
        IServiceScopeFactory scopeFactory,
        LocalServerRuntimeAutomationOptions options,
        ILogger<LocalServerRuntimeWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableBackgroundWorker)
        {
            _logger.LogInformation("SafarSuite local-server background worker is disabled.");
            return;
        }

        var lastPullAtUtc = DateTimeOffset.MinValue;
        var lastHeartbeatAtUtc = DateTimeOffset.MinValue;
        var lastCommandPollAtUtc = DateTimeOffset.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            if (now - lastCommandPollAtUtc >= _options.CommandPollInterval)
            {
                await ProcessCommandsAsync(stoppingToken);
                lastCommandPollAtUtc = now;
            }

            if (now - lastPullAtUtc >= _options.EntitlementPullInterval)
            {
                await PullEntitlementAsync(stoppingToken);
                lastPullAtUtc = now;
            }

            if (now - lastHeartbeatAtUtc >= _options.HeartbeatInterval)
            {
                await ReportHeartbeatAsync(stoppingToken);
                lastHeartbeatAtUtc = now;
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ProcessCommandsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ProcessInstallationCommandsFromBootstrapConfigurationHandler>();
            var result = await handler.HandleAsync(
                new ProcessInstallationCommandsFromBootstrapConfigurationCommand(),
                cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "Local command polling did not complete: {FailureCode} {Detail}",
                    result.FailureCode,
                    result.Detail);
                return;
            }

            if (result.PendingCommandCount == 0)
            {
                _logger.LogDebug("No pending local commands were returned by Control Cloud.");
                return;
            }

            _logger.LogInformation(
                "Processed {CommandCount} local command(s): {AppliedCount} applied, {FailedCount} failed, {RejectedCount} rejected.",
                result.PendingCommandCount,
                result.AppliedCount,
                result.FailedCount,
                result.RejectedCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Local command polling failed.");
        }
    }

    private async Task PullEntitlementAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<PullEntitlementFromBootstrapConfigurationHandler>();
            var result = await handler.HandleAsync(
                new PullEntitlementFromBootstrapConfigurationCommand(),
                cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "Local entitlement pull did not complete: {FailureCode} {Detail}",
                    result.FailureCode,
                    result.Detail);
                return;
            }

            _logger.LogInformation(
                "Pulled local entitlement version {EntitlementVersion} for installation {InstallationId}.",
                result.Entitlement!.EntitlementVersion,
                result.Entitlement.InstallationId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Local entitlement pull failed.");
        }
    }

    private async Task ReportHeartbeatAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ReportHeartbeatFromBootstrapConfigurationHandler>();
            var result = await handler.HandleAsync(
                new ReportHeartbeatFromBootstrapConfigurationCommand(
                    Detail: "Automated local-server heartbeat."),
                cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "Local heartbeat did not complete: {FailureCode} {Detail}",
                    result.FailureCode,
                    result.Detail);
                return;
            }

            _logger.LogInformation(
                "Reported local heartbeat {HeartbeatStatus} for installation {InstallationId}.",
                result.Heartbeat!.HeartbeatStatus,
                result.Heartbeat.InstallationId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Local heartbeat failed.");
        }
    }
}
