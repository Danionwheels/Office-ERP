using Microsoft.Extensions.Options;
using SafarSuite.ControlDesk.Application.Common.Abstractions;

namespace SafarSuite.ControlDesk.Api.Modules.ControlCloud;

public sealed class ControlCloudOutboxPublisherWorker : BackgroundService
{
    private readonly ControlCloudOutboxPublishCoordinator _coordinator;
    private readonly CloudOutboxAutomationState _state;
    private readonly ControlCloudOutboxWorkerOptions _options;
    private readonly IClock _clock;
    private readonly ILogger<ControlCloudOutboxPublisherWorker> _logger;

    public ControlCloudOutboxPublisherWorker(
        ControlCloudOutboxPublishCoordinator coordinator,
        CloudOutboxAutomationState state,
        IOptions<ControlCloudOutboxWorkerOptions> options,
        IClock clock,
        ILogger<ControlCloudOutboxPublisherWorker> logger)
    {
        _coordinator = coordinator;
        _state = state;
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _state.Start(_options.Enabled, _clock.UtcNow);

        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "Control Desk outbox automation is disabled. EventCode={EventCode}",
                "OfficeOutboxWorkerDisabled");
            return;
        }

        if (!_coordinator.IsAutomaticPublisherConfigured)
        {
            _state.FailCycle("OutboxPublisherNotConfigured", _clock.UtcNow);
            _logger.LogWarning(
                "Control Desk outbox automation requires a configured HTTP Control Cloud publisher. EventCode={EventCode}",
                "OfficeOutboxPublisherNotConfigured");
            return;
        }

        _logger.LogInformation(
            "Control Desk outbox automation started with batch size {BatchSize} and poll interval {PollIntervalSeconds}. EventCode={EventCode}",
            _options.BatchSize,
            _options.PollIntervalSeconds,
            "OfficeOutboxWorkerStarted");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await RunCycleAsync(stoppingToken);

                await Task.Delay(
                    TimeSpan.FromSeconds(_options.PollIntervalSeconds),
                    stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // A host-requested stop is the normal service lifecycle.
        }
        finally
        {
            _state.Stop(_clock.UtcNow);
            _logger.LogInformation(
                "Control Desk outbox automation stopped. EventCode={EventCode}",
                "OfficeOutboxWorkerStopped");
        }
    }

    public async Task RunCycleAsync(CancellationToken cancellationToken = default)
    {
        _state.BeginCycle(_clock.UtcNow);

        try
        {
            var result = await _coordinator.PublishAsync(
                _options.BatchSize,
                cancellationToken);

            if (result.IsFailure)
            {
                _state.FailCycle("OutboxBatchRejected", _clock.UtcNow);
                _logger.LogError(
                    "Control Desk outbox batch was rejected. EventCode={EventCode}",
                    "OfficeOutboxBatchRejected");
                return;
            }

            _state.CompleteCycle(result.Value, _clock.UtcNow);

            if (result.Value.Messages.Count > 0)
            {
                _logger.LogInformation(
                    "Control Desk outbox cycle completed. PublishedCount={PublishedCount} FailedCount={FailedCount} EventCode={EventCode}",
                    result.Value.PublishedCount,
                    result.Value.FailedCount,
                    "OfficeOutboxBatchCompleted");
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _state.FailCycle("OutboxCycleFailed", _clock.UtcNow);
            _logger.LogError(
                "Control Desk outbox cycle failed. ExceptionType={ExceptionType} EventCode={EventCode}",
                exception.GetType().FullName,
                "OfficeOutboxCycleFailed");
        }
    }

}
