using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SafarSuite.ControlDesk.Api.Modules.ControlCloud;
using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.PublishPendingCloudOutboxMessages;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;
using SafarSuite.ControlDesk.Infrastructure.ControlCloud;
using SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class ControlCloudOutboxAutomationTests
{
    private static readonly DateTimeOffset OccurredAtUtc =
        new(2026, 7, 18, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Worker_recovers_automatically_after_cloud_reconnect_without_duplicate_acceptance()
    {
        var repository = new InMemoryCloudOutboxMessageRepository();
        var message = CreateMessage();
        await repository.AddAsync(message);
        var publisher = new RecoveringIdempotentPublisher();
        var clock = new MutableClock(OccurredAtUtc.AddMinutes(1));
        await using var services = BuildServices(repository, publisher, clock);
        using var coordinator = CreateCoordinator(services);
        var state = new CloudOutboxAutomationState();
        var worker = CreateWorker(coordinator, state, clock);

        await worker.StartAsync(CancellationToken.None);

        try
        {
            await WaitUntilAsync(
                () => message.Status == CloudOutboxMessageStatus.Failed,
                TimeSpan.FromSeconds(5));
            Assert.Equal(1, message.AttemptCount);
            Assert.NotNull(message.NextAttemptAtUtc);

            publisher.IsReachable = true;
            clock.UtcNow = message.NextAttemptAtUtc!.Value;
            await WaitUntilAsync(
                () => message.Status == CloudOutboxMessageStatus.Sent,
                TimeSpan.FromSeconds(5));
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
            worker.Dispose();
        }

        Assert.Equal(CloudOutboxMessageStatus.Sent, message.Status);
        Assert.Equal(2, message.AttemptCount);
        Assert.Equal(2, publisher.CallCount);
        Assert.Single(publisher.AcceptedMessageIds);
        Assert.Equal("Stopped", state.GetSnapshot().Status);
        Assert.NotNull(state.GetSnapshot().LastPublishSucceededAtUtc);
    }

    [Fact]
    public async Task Coordinator_serializes_simultaneous_manual_and_worker_batches()
    {
        var repository = new InMemoryCloudOutboxMessageRepository();
        await repository.AddAsync(CreateMessage());
        await repository.AddAsync(CreateMessage());
        var publisher = new BlockingPublisher();
        var clock = new MutableClock(OccurredAtUtc.AddMinutes(1));
        await using var services = BuildServices(repository, publisher, clock);
        using var coordinator = CreateCoordinator(services);

        var first = coordinator.PublishAsync(batchSize: 1);
        await publisher.FirstCallEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = coordinator.PublishAsync(batchSize: 1);

        await Task.Delay(100);
        Assert.Equal(1, publisher.CallCount);
        Assert.Equal(1, publisher.MaximumConcurrentCalls);

        publisher.ReleaseCalls.TrySetResult();
        var results = await Task.WhenAll(first, second);

        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Equal(2, publisher.CallCount);
        Assert.Equal(1, publisher.MaximumConcurrentCalls);
    }

    [Fact]
    public async Task Worker_recovers_when_cloud_accepts_before_local_save_without_duplicate_acceptance()
    {
        var store = new FailFirstSaveOutboxStore();
        var publisher = new IdempotentAcceptingPublisher();
        var clock = new MutableClock(OccurredAtUtc.AddMinutes(1));
        var registrations = new ServiceCollection();
        registrations.AddSingleton<ICloudOutboxMessageRepository>(store);
        registrations.AddSingleton<IUnitOfWork>(store);
        registrations.AddSingleton<ICloudOutboxPublisher>(publisher);
        registrations.AddSingleton<IClock>(clock);
        registrations.AddSingleton<ICloudOutboxPublishPolicy>(
            new TestPublishPolicy(0, TimeSpan.FromSeconds(5)));
        registrations.AddSingleton<ICloudOutboxPublisherAvailability>(
            AvailablePublisher());
        registrations.AddSingleton<
            ICloudOutboxPublicationLeaseProvider,
            InMemoryCloudOutboxPublicationLeaseProvider>();
        registrations.AddScoped<PublishPendingCloudOutboxMessagesHandler>();
        await using var services = registrations.BuildServiceProvider();
        using var coordinator = CreateCoordinator(services);
        var state = new CloudOutboxAutomationState();
        var worker = CreateWorker(coordinator, state, clock);
        state.Start(enabled: true, clock.UtcNow);

        await worker.RunCycleAsync();
        Assert.False(store.IsCommitted);
        Assert.Equal("Faulted", state.GetSnapshot().Status);

        await worker.RunCycleAsync();
        await worker.RunCycleAsync();

        Assert.True(store.IsCommitted);
        Assert.Equal(2, publisher.CallCount);
        Assert.Equal(1, publisher.NewAcceptanceCount);
        Assert.Equal(1, publisher.DuplicateAcceptanceCount);
    }

    [Fact]
    public async Task Coordinator_honors_cancellation_while_waiting_for_publication_gate()
    {
        var repository = new InMemoryCloudOutboxMessageRepository();
        await repository.AddAsync(CreateMessage());
        await repository.AddAsync(CreateMessage());
        var publisher = new BlockingPublisher();
        var clock = new MutableClock(OccurredAtUtc.AddMinutes(1));
        await using var services = BuildServices(repository, publisher, clock);
        using var coordinator = CreateCoordinator(services);

        var first = coordinator.PublishAsync(batchSize: 1);
        await publisher.FirstCallEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            coordinator.PublishAsync(batchSize: 1, cancellation.Token));

        publisher.ReleaseCalls.TrySetResult();
        Assert.True((await first).IsSuccess);
        Assert.Equal(1, publisher.MaximumConcurrentCalls);
    }

    [Fact]
    public async Task Production_cleartext_publisher_is_rejected_before_dequeue()
    {
        var repository = new InMemoryCloudOutboxMessageRepository();
        var message = CreateMessage();
        await repository.AddAsync(message);
        var publisher = new RecoveringIdempotentPublisher { IsReachable = true };
        var clock = new MutableClock(OccurredAtUtc.AddMinutes(1));
        var options = Options.Create(new ControlCloudPublisherOptions
        {
            Mode = "Http",
            SourceSystem = "SafarSuite.ControlDesk",
            Environment = "Production",
            SigningKeyId = "production-key",
            SigningSecret = "production-signing-secret-at-least-32-characters",
            EndpointUrl = "http://cloud.example.test/api/v1/control-desk/messages"
        });
        var availability = new ConfiguredCloudOutboxPublisherAvailability(
            options,
            new TestHostEnvironment("Production"));
        await using var services = BuildServices(
            repository,
            publisher,
            clock,
            availability);
        using var coordinator = CreateCoordinator(services);

        var result = await coordinator.PublishAsync(batchSize: 1);

        Assert.True(result.IsFailure);
        Assert.Equal(CloudOutboxMessageStatus.Pending, message.Status);
        Assert.Equal(0, message.AttemptCount);
        Assert.Equal(0, publisher.CallCount);
    }

    [Fact]
    public async Task Production_development_signing_values_are_rejected_before_dequeue()
    {
        var repository = new InMemoryCloudOutboxMessageRepository();
        var message = CreateMessage();
        await repository.AddAsync(message);
        var publisher = new RecoveringIdempotentPublisher { IsReachable = true };
        var clock = new MutableClock(OccurredAtUtc.AddMinutes(1));
        var options = Options.Create(new ControlCloudPublisherOptions
        {
            Mode = "Http",
            SourceSystem = "SafarSuite.ControlDesk",
            Environment = "Local",
            SigningKeyId = "local-dev",
            SigningSecret = "local-development-signing-secret-change-before-cloud",
            EndpointUrl = "https://cloud.example.test/api/v1/control-desk/messages"
        });
        var availability = new ConfiguredCloudOutboxPublisherAvailability(
            options,
            new TestHostEnvironment("Production"));
        await using var services = BuildServices(
            repository,
            publisher,
            clock,
            availability);
        using var coordinator = CreateCoordinator(services);

        var result = await coordinator.PublishAsync(batchSize: 1);

        Assert.True(result.IsFailure);
        Assert.Equal(CloudOutboxMessageStatus.Pending, message.Status);
        Assert.Equal(0, message.AttemptCount);
        Assert.Equal(0, publisher.CallCount);
    }

    private static ServiceProvider BuildServices(
        ICloudOutboxMessageRepository repository,
        ICloudOutboxPublisher publisher,
        IClock clock,
        ICloudOutboxPublisherAvailability? availability = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(repository);
        services.AddSingleton(publisher);
        services.AddSingleton<IClock>(clock);
        services.AddSingleton<ICloudOutboxPublishPolicy>(
            new TestPublishPolicy(0, TimeSpan.FromSeconds(5)));
        services.AddSingleton(availability ?? AvailablePublisher());
        services.AddSingleton<
            ICloudOutboxPublicationLeaseProvider,
            InMemoryCloudOutboxPublicationLeaseProvider>();
        services.AddSingleton<IUnitOfWork, NoOpUnitOfWork>();
        services.AddScoped<PublishPendingCloudOutboxMessagesHandler>();

        return services.BuildServiceProvider();
    }

    private static ControlCloudOutboxPublishCoordinator CreateCoordinator(
        IServiceProvider services) =>
        new(
            services.GetRequiredService<IServiceScopeFactory>(),
            services.GetRequiredService<ICloudOutboxPublisherAvailability>());

    private static ICloudOutboxPublisherAvailability AvailablePublisher() =>
        new StaticPublisherAvailability(
            new CloudOutboxPublisherAvailabilitySnapshot(true, true, "Ready"));

    private static ControlCloudOutboxPublisherWorker CreateWorker(
        ControlCloudOutboxPublishCoordinator coordinator,
        CloudOutboxAutomationState state,
        IClock clock) =>
        new(
            coordinator,
            state,
            Options.Create(new ControlCloudOutboxWorkerOptions
            {
                Enabled = true,
                BatchSize = 20,
                PollIntervalSeconds = 1
            }),
            clock,
            NullLogger<ControlCloudOutboxPublisherWorker>.Instance);

    private static CloudOutboxMessage CreateMessage() =>
        CloudOutboxMessage.Create(
            CloudOutboxMessageId.Create(Guid.NewGuid()),
            ClientId.Create(Guid.NewGuid()),
            "TestMessage",
            "TestSubject",
            Guid.NewGuid().ToString("D"),
            "{\"value\":1}",
            OccurredAtUtc);

    private static async Task WaitUntilAsync(
        Func<bool> condition,
        TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (!condition())
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException("The expected outbox state transition did not occur.");
            }

            await Task.Delay(25);
        }
    }

    private sealed record TestPublishPolicy(
        int MaximumAttemptCount,
        TimeSpan RetryDelay) : ICloudOutboxPublishPolicy;

    private sealed record StaticPublisherAvailability(
        CloudOutboxPublisherAvailabilitySnapshot Snapshot)
        : ICloudOutboxPublisherAvailability
    {
        public CloudOutboxPublisherAvailabilitySnapshot GetSnapshot() => Snapshot;
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "SafarSuite.ControlDesk.Api.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class MutableClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;

        public DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);
    }

    private sealed class RecoveringIdempotentPublisher : ICloudOutboxPublisher
    {
        private readonly ConcurrentDictionary<Guid, byte> _acceptedMessageIds = new();

        public bool IsReachable { get; set; }

        public int CallCount { get; private set; }

        public IReadOnlyCollection<Guid> AcceptedMessageIds => _acceptedMessageIds.Keys.ToArray();

        public Task<CloudOutboxPublishResult> PublishAsync(
            CloudOutboxMessage message,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            if (!IsReachable)
            {
                return Task.FromResult(CloudOutboxPublishResult.Failure("Cloud unavailable."));
            }

            _acceptedMessageIds.TryAdd(message.Id.Value, 0);

            return Task.FromResult(CloudOutboxPublishResult.Success(
                $"cloud-{message.Id.Value:N}",
                "test-signature"));
        }
    }

    private sealed class BlockingPublisher : ICloudOutboxPublisher
    {
        private int _activeCalls;
        private int _callCount;
        private int _maximumConcurrentCalls;

        public TaskCompletionSource FirstCallEntered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleaseCalls { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int CallCount => Volatile.Read(ref _callCount);

        public int MaximumConcurrentCalls => Volatile.Read(ref _maximumConcurrentCalls);

        public async Task<CloudOutboxPublishResult> PublishAsync(
            CloudOutboxMessage message,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _callCount);
            var activeCalls = Interlocked.Increment(ref _activeCalls);
            UpdateMaximum(activeCalls);
            FirstCallEntered.TrySetResult();

            try
            {
                await ReleaseCalls.Task.WaitAsync(cancellationToken);

                return CloudOutboxPublishResult.Success(
                    $"cloud-{message.Id.Value:N}",
                    "test-signature");
            }
            finally
            {
                Interlocked.Decrement(ref _activeCalls);
            }
        }

        private void UpdateMaximum(int activeCalls)
        {
            while (true)
            {
                var current = Volatile.Read(ref _maximumConcurrentCalls);
                if (activeCalls <= current
                    || Interlocked.CompareExchange(
                        ref _maximumConcurrentCalls,
                        activeCalls,
                        current) == current)
                {
                    return;
                }
            }
        }
    }

    private sealed class IdempotentAcceptingPublisher : ICloudOutboxPublisher
    {
        private readonly HashSet<Guid> _accepted = [];

        public int CallCount { get; private set; }

        public int NewAcceptanceCount { get; private set; }

        public int DuplicateAcceptanceCount { get; private set; }

        public Task<CloudOutboxPublishResult> PublishAsync(
            CloudOutboxMessage message,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            if (_accepted.Add(message.Id.Value))
            {
                NewAcceptanceCount++;
            }
            else
            {
                DuplicateAcceptanceCount++;
            }

            return Task.FromResult(CloudOutboxPublishResult.Success(
                $"cloud-{message.Id.Value:N}",
                "test-signature"));
        }
    }

    private sealed class FailFirstSaveOutboxStore
        : ICloudOutboxMessageRepository, IUnitOfWork
    {
        private readonly CloudOutboxMessageId _messageId =
            CloudOutboxMessageId.Create(Guid.NewGuid());
        private int _saveCount;

        public bool IsCommitted { get; private set; }

        public Task AddAsync(
            CloudOutboxMessage message,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<CloudOutboxMessage?> GetByIdAsync(
            CloudOutboxMessageId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CloudOutboxMessage?>(null);

        public Task<IReadOnlyCollection<CloudOutboxMessage>> ListPageAsync(
            CloudOutboxMessageStatus? status,
            string? messageType,
            ClientId? clientId,
            DateTimeOffset? beforeOccurredAtUtc,
            CloudOutboxMessageId? beforeMessageId,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<CloudOutboxMessage>>([]);

        public Task<CloudOutboxMessageRegisterSummary> SummarizeAsync(
            CloudOutboxMessageStatus? status,
            string? messageType,
            ClientId? clientId,
            DateTimeOffset readyAtUtc,
            int maximumAttemptCount,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CloudOutboxMessageRegisterSummary.Empty);

        public Task<IReadOnlyCollection<CloudOutboxMessage>> ListReadyForPublishingAsync(
            int batchSize,
            DateTimeOffset readyAtUtc,
            int maximumAttemptCount,
            CancellationToken cancellationToken = default)
        {
            if (IsCommitted)
            {
                return Task.FromResult<IReadOnlyCollection<CloudOutboxMessage>>([]);
            }

            var message = CloudOutboxMessage.Create(
                _messageId,
                ClientId.Create(Guid.Parse("8a8adc3a-a8a9-4d94-a769-810ed59ba314")),
                "TestMessage",
                "TestSubject",
                "test-subject",
                "{\"value\":1}",
                OccurredAtUtc);

            return Task.FromResult<IReadOnlyCollection<CloudOutboxMessage>>([message]);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _saveCount) == 1)
            {
                throw new InvalidOperationException("Simulated local acknowledgement loss.");
            }

            IsCommitted = true;
            return Task.CompletedTask;
        }

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            await operation(cancellationToken);
        }

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            return await operation(cancellationToken);
        }
    }
}
