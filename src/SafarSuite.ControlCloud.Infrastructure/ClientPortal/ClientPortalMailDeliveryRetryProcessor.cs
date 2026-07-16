using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class ClientPortalMailDeliveryRetryProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClientPortalMailTransport _transport;
    private readonly IControlCloudClock _clock;
    private readonly ClientPortalInvitationDeliveryOptions _options;
    private readonly ILogger<ClientPortalMailDeliveryRetryProcessor> _logger;
    private readonly ControlCloudClientPortalMailRetryPolicy _retryPolicy = new();

    public ClientPortalMailDeliveryRetryProcessor(
        IServiceScopeFactory scopeFactory,
        IClientPortalMailTransport transport,
        IControlCloudClock clock,
        ClientPortalInvitationDeliveryOptions options,
        ILogger<ClientPortalMailDeliveryRetryProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _transport = transport;
        _clock = clock;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "The client portal mail delivery polling cycle failed.");
            }

            try
            {
                await Task.Delay(GetPollInterval(), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    public async Task<int> ProcessDueBatchAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IClientPortalMailDeliveryRepository>();
        var claimed = await repository.ClaimDueAsync(
            _clock.UtcNow,
            GetClaimLeaseDuration(),
            Math.Clamp(_options.MailQueueBatchSize, 1, 500),
            cancellationToken);

        foreach (var delivery in claimed)
        {
            await ProcessDeliveryAsync(repository, delivery, cancellationToken);
        }

        return claimed.Count;
    }

    private async Task ProcessDeliveryAsync(
        IClientPortalMailDeliveryRepository repository,
        ControlCloudClientPortalMailDelivery delivery,
        CancellationToken cancellationToken)
    {
        var leaseId = delivery.LeaseId
            ?? throw new InvalidOperationException(
                $"Claimed mail delivery '{delivery.DeliveryId}' has no lease id.");

        try
        {
            await _transport.SendAsync(
                new ClientPortalMailMessage(
                    delivery.DeliveryId,
                    delivery.ClientId,
                    delivery.RecipientEmail,
                    delivery.RecipientName,
                    delivery.Subject,
                    delivery.TextBody),
                cancellationToken);
            delivery.MarkSent(leaseId, _clock.UtcNow);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var attemptedAtUtc = _clock.UtcNow;
            var nextAttemptAtUtc = _retryPolicy.GetNextAttemptAtUtc(
                attemptedAtUtc,
                delivery.AttemptCount + 1,
                GetInitialRetryDelay());
            delivery.MarkAttemptFailed(
                leaseId,
                attemptedAtUtc,
                exception.Message,
                nextAttemptAtUtc);

            if (nextAttemptAtUtc is null)
            {
                _logger.LogError(
                    exception,
                    "Mail delivery {DeliveryId} reached terminal failure after {AttemptCount} attempts.",
                    delivery.DeliveryId,
                    delivery.AttemptCount);
            }
            else
            {
                _logger.LogWarning(
                    exception,
                    "Mail delivery {DeliveryId} failed on attempt {AttemptCount}; next attempt is {NextAttemptAtUtc}.",
                    delivery.DeliveryId,
                    delivery.AttemptCount,
                    nextAttemptAtUtc);
            }
        }

        try
        {
            await repository.SaveAsync(delivery, leaseId, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Could not persist the outcome for mail delivery {DeliveryId}; its lease will expire for recovery.",
                delivery.DeliveryId);
        }
    }

    private TimeSpan GetPollInterval()
    {
        return TimeSpan.FromSeconds(Math.Clamp(_options.MailQueuePollIntervalSeconds, 1, 300));
    }

    private TimeSpan GetClaimLeaseDuration()
    {
        return TimeSpan.FromSeconds(Math.Clamp(_options.MailQueueClaimLeaseSeconds, 30, 3_600));
    }

    private TimeSpan GetInitialRetryDelay()
    {
        return TimeSpan.FromSeconds(Math.Clamp(_options.MailQueueInitialRetryDelaySeconds, 1, 86_400));
    }
}
