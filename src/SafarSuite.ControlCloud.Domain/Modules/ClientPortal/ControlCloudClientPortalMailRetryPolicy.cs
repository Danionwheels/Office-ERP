namespace SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

public sealed class ControlCloudClientPortalMailRetryPolicy
{
    public const int MaximumRetryCount = 3;

    public const int MaximumAttemptCount = MaximumRetryCount + 1;

    public DateTimeOffset? GetNextAttemptAtUtc(
        DateTimeOffset failedAtUtc,
        int completedAttemptCount,
        TimeSpan initialRetryDelay)
    {
        if (completedAttemptCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(completedAttemptCount),
                "Completed attempt count must be positive.");
        }

        if (initialRetryDelay <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialRetryDelay),
                "Initial retry delay must be positive.");
        }

        if (completedAttemptCount >= MaximumAttemptCount)
        {
            return null;
        }

        var multiplier = 1L << (completedAttemptCount - 1);
        var maximumDelayTicks = TimeSpan.FromDays(30).Ticks;
        var delayTicks = initialRetryDelay.Ticks > maximumDelayTicks / multiplier
            ? maximumDelayTicks
            : initialRetryDelay.Ticks * multiplier;

        return failedAtUtc.AddTicks(delayTicks);
    }
}
