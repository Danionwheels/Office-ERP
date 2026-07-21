using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;

namespace SafarSuite.ControlDesk.Domain.Tests;

public sealed class CloudOutboxMessageRetryTests
{
    private static readonly DateTimeOffset OccurredAtUtc =
        new(2026, 7, 18, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Zero_maximum_keeps_due_transient_failure_ready_after_many_attempts()
    {
        var message = CreateMessage();
        var attemptedAtUtc = OccurredAtUtc;

        for (var attempt = 0; attempt < 8; attempt++)
        {
            attemptedAtUtc = attemptedAtUtc.AddMinutes(1);
            message.MarkFailed("Cloud unavailable.", attemptedAtUtc, attemptedAtUtc.AddMinutes(1));
        }

        Assert.Equal(8, message.AttemptCount);
        Assert.True(message.IsReadyForPublishing(attemptedAtUtc.AddMinutes(1), maximumAttemptCount: 0));
    }

    [Fact]
    public void Positive_maximum_still_caps_due_transient_failure()
    {
        var message = CreateMessage();
        var firstAttemptAtUtc = OccurredAtUtc.AddMinutes(1);
        var secondAttemptAtUtc = OccurredAtUtc.AddMinutes(2);

        message.MarkFailed("First failure.", firstAttemptAtUtc, secondAttemptAtUtc);
        message.MarkFailed("Second failure.", secondAttemptAtUtc, secondAttemptAtUtc.AddMinutes(1));

        Assert.False(message.IsReadyForPublishing(
            secondAttemptAtUtc.AddMinutes(1),
            maximumAttemptCount: 2));
    }

    [Fact]
    public void Permanent_failure_remains_terminal_under_unlimited_policy()
    {
        var message = CreateMessage();
        var failedAtUtc = OccurredAtUtc.AddMinutes(1);

        message.MarkFailed("Permanent failure.", failedAtUtc, nextAttemptAtUtc: null);

        Assert.False(message.IsReadyForPublishing(failedAtUtc.AddDays(1), maximumAttemptCount: 0));
    }

    [Fact]
    public void Pending_message_is_ready_under_unlimited_policy()
    {
        var message = CreateMessage();

        Assert.True(message.IsReadyForPublishing(OccurredAtUtc, maximumAttemptCount: 0));
    }

    [Fact]
    public void Negative_maximum_is_rejected()
    {
        var message = CreateMessage();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            message.IsReadyForPublishing(OccurredAtUtc, maximumAttemptCount: -1));
    }

    [Fact]
    public void Failure_reason_is_trimmed_to_persistence_contract()
    {
        var message = CreateMessage();
        var oversizedReason = $"  {new string('x', CloudOutboxMessage.MaximumFailureReasonLength + 25)}  ";

        message.MarkFailed(oversizedReason, OccurredAtUtc.AddMinutes(1), nextAttemptAtUtc: null);

        Assert.NotNull(message.FailureReason);
        Assert.Equal(CloudOutboxMessage.MaximumFailureReasonLength, message.FailureReason!.Length);
        Assert.DoesNotContain(' ', message.FailureReason);
    }

    private static CloudOutboxMessage CreateMessage() =>
        CloudOutboxMessage.Create(
            CloudOutboxMessageId.Create(Guid.NewGuid()),
            ClientId.Create(Guid.NewGuid()),
            "TestMessage",
            "TestSubject",
            Guid.NewGuid().ToString("D"),
            "{\"value\":1}",
            OccurredAtUtc);
}
