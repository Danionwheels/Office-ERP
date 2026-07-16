using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Identity.Tests;

public sealed class ClientPortalMailRetryPolicyTests
{
    [Fact]
    public void FailuresUseExponentialBackoffThenBecomeTerminalAfterThreeRetries()
    {
        var policy = new ControlCloudClientPortalMailRetryPolicy();
        var initialDelay = TimeSpan.FromMinutes(1);
        var attemptedAt = new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero);
        var delivery = ControlCloudClientPortalMailDelivery.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "billing@example.test",
            "Billing Contact",
            "Password reset",
            "Reset your password.",
            attemptedAt);

        for (var completedAttemptCount = 1;
             completedAttemptCount <= ControlCloudClientPortalMailRetryPolicy.MaximumAttemptCount;
             completedAttemptCount++)
        {
            var leaseId = Guid.NewGuid();
            delivery.Claim(leaseId, attemptedAt, attemptedAt.AddMinutes(1));
            var nextAttempt = policy.GetNextAttemptAtUtc(
                attemptedAt,
                completedAttemptCount,
                initialDelay);
            delivery.MarkAttemptFailed(
                leaseId,
                attemptedAt,
                $"failure-{completedAttemptCount}",
                nextAttempt);

            Assert.Equal(completedAttemptCount, delivery.AttemptCount);

            if (completedAttemptCount < ControlCloudClientPortalMailRetryPolicy.MaximumAttemptCount)
            {
                var expectedDelay = TimeSpan.FromTicks(
                    initialDelay.Ticks * (1L << (completedAttemptCount - 1)));
                Assert.Equal(attemptedAt.Add(expectedDelay), nextAttempt);
                Assert.Equal(ControlCloudClientPortalMailDeliveryStatuses.Pending, delivery.Status);
                attemptedAt = nextAttempt!.Value;
            }
            else
            {
                Assert.Null(nextAttempt);
                Assert.Equal(ControlCloudClientPortalMailDeliveryStatuses.Failed, delivery.Status);
                Assert.Equal(attemptedAt, delivery.FailedAtUtc);
            }
        }

        Assert.Equal(4, delivery.AttemptCount);
    }
}
