using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.PublishPendingCloudOutboxMessages;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;
using SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class CloudOutboxPublishingRetryTests
{
    private static readonly DateTimeOffset OccurredAtUtc =
        new(2026, 7, 18, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handler_schedules_retry_after_many_attempts_when_maximum_is_zero()
    {
        var message = CreateMessage();
        var lastAttemptAtUtc = OccurredAtUtc;

        for (var attempt = 0; attempt < 6; attempt++)
        {
            lastAttemptAtUtc = lastAttemptAtUtc.AddMinutes(1);
            message.MarkFailed("Cloud unavailable.", lastAttemptAtUtc, lastAttemptAtUtc.AddMinutes(1));
        }

        var clock = new FixedClock(lastAttemptAtUtc.AddMinutes(1));
        var repository = new InMemoryCloudOutboxMessageRepository();
        await repository.AddAsync(message);
        var publisher = new StubPublisher(CloudOutboxPublishResult.Failure("Still unavailable."));
        var retryDelay = TimeSpan.FromMinutes(3);
        var handler = CreateHandler(repository, publisher, maximumAttemptCount: 0, retryDelay, clock);

        var result = await handler.HandleAsync(new PublishPendingCloudOutboxMessagesCommand(20));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.FailedCount);
        Assert.Equal(7, message.AttemptCount);
        Assert.Equal(clock.UtcNow.Add(retryDelay), message.NextAttemptAtUtc);
        Assert.Equal(1, publisher.CallCount);
    }

    [Fact]
    public async Task Handler_keeps_nonretryable_failure_terminal_under_unlimited_policy()
    {
        var message = CreateMessage();
        var clock = new FixedClock(OccurredAtUtc.AddMinutes(1));
        var repository = new InMemoryCloudOutboxMessageRepository();
        await repository.AddAsync(message);
        var publisher = new StubPublisher(
            CloudOutboxPublishResult.Failure("Rejected.", shouldRetry: false));
        var handler = CreateHandler(
            repository,
            publisher,
            maximumAttemptCount: 0,
            TimeSpan.FromMinutes(1),
            clock);

        var firstResult = await handler.HandleAsync(new PublishPendingCloudOutboxMessagesCommand(20));
        clock.UtcNow = clock.UtcNow.AddDays(1);
        var secondResult = await handler.HandleAsync(new PublishPendingCloudOutboxMessagesCommand(20));

        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsSuccess);
        Assert.Null(message.NextAttemptAtUtc);
        Assert.Equal(CloudOutboxMessageStatus.Failed, message.Status);
        Assert.Equal(1, publisher.CallCount);
        Assert.Empty(secondResult.Value!.Messages);
    }

    [Fact]
    public async Task InMemory_repository_summary_and_ready_query_honor_unlimited_and_positive_maximums()
    {
        var readyAtUtc = OccurredAtUtc.AddHours(1);
        var repository = new InMemoryCloudOutboxMessageRepository();
        var exhaustedUnderPositiveCap = CreateMessage();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var attemptedAtUtc = OccurredAtUtc.AddMinutes(attempt + 1);
            exhaustedUnderPositiveCap.MarkFailed(
                "Transient.",
                attemptedAtUtc,
                attemptedAtUtc.AddMinutes(1));
        }

        var pending = CreateMessage();
        var terminal = CreateMessage();
        terminal.MarkFailed("Permanent.", OccurredAtUtc.AddMinutes(1), nextAttemptAtUtc: null);
        await repository.AddAsync(exhaustedUnderPositiveCap);
        await repository.AddAsync(pending);
        await repository.AddAsync(terminal);

        var unlimitedSummary = await repository.SummarizeAsync(
            status: null,
            messageType: null,
            clientId: null,
            readyAtUtc,
            maximumAttemptCount: 0);
        var unlimitedReady = await repository.ListReadyForPublishingAsync(
            batchSize: 20,
            readyAtUtc,
            maximumAttemptCount: 0);
        var cappedSummary = await repository.SummarizeAsync(
            status: null,
            messageType: null,
            clientId: null,
            readyAtUtc,
            maximumAttemptCount: 5);
        var cappedReady = await repository.ListReadyForPublishingAsync(
            batchSize: 20,
            readyAtUtc,
            maximumAttemptCount: 5);

        Assert.Equal(2, unlimitedSummary.ReadyForPublishingCount);
        Assert.Equal(2, unlimitedReady.Count);
        Assert.Equal(1, cappedSummary.ReadyForPublishingCount);
        Assert.Single(cappedReady);
        Assert.Equal(pending.Id, cappedReady.Single().Id);
    }

    private static PublishPendingCloudOutboxMessagesHandler CreateHandler(
        ICloudOutboxMessageRepository repository,
        ICloudOutboxPublisher publisher,
        int maximumAttemptCount,
        TimeSpan retryDelay,
        IClock clock) =>
        new(
            repository,
            publisher,
            new AvailablePublisher(),
            new InMemoryCloudOutboxPublicationLeaseProvider(),
            new StubPublishPolicy(maximumAttemptCount, retryDelay),
            new NoOpUnitOfWork(),
            clock);

    private static CloudOutboxMessage CreateMessage() =>
        CloudOutboxMessage.Create(
            CloudOutboxMessageId.Create(Guid.NewGuid()),
            ClientId.Create(Guid.NewGuid()),
            "TestMessage",
            "TestSubject",
            Guid.NewGuid().ToString("D"),
            "{\"value\":1}",
            OccurredAtUtc);

    private sealed class StubPublisher(CloudOutboxPublishResult result) : ICloudOutboxPublisher
    {
        public int CallCount { get; private set; }

        public Task<CloudOutboxPublishResult> PublishAsync(
            CloudOutboxMessage message,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(result);
        }
    }

    private sealed record StubPublishPolicy(
        int MaximumAttemptCount,
        TimeSpan RetryDelay) : ICloudOutboxPublishPolicy;

    private sealed class AvailablePublisher : ICloudOutboxPublisherAvailability
    {
        public CloudOutboxPublisherAvailabilitySnapshot GetSnapshot() =>
            new(true, true, "Ready");
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;

        public DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);
    }
}
