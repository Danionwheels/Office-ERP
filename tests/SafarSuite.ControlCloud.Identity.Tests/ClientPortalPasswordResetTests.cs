using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Identity.Tests;

public sealed class ClientPortalPasswordResetTests
{
    [Fact]
    public void ResetExpiresAndCanBeConsumedOnlyOnce()
    {
        var now = new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero);
        var expiresAt = now.AddMinutes(30);
        var reset = ControlCloudClientPortalPasswordReset.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "reset-token-hash",
            now,
            expiresAt);

        Assert.True(reset.IsUsableAt(now.AddMinutes(29)));
        Assert.True(reset.TryConsume(now.AddMinutes(1)));
        Assert.False(reset.TryConsume(now.AddMinutes(2)));
        Assert.False(reset.IsUsableAt(expiresAt));
    }

    [Fact]
    public void UnusedResetIsRejectedAtItsExpiryBoundary()
    {
        var now = new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero);
        var expiresAt = now.AddMinutes(30);
        var reset = ControlCloudClientPortalPasswordReset.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "reset-token-hash",
            now,
            expiresAt);

        Assert.False(reset.IsUsableAt(expiresAt));
        Assert.False(reset.TryConsume(expiresAt));
        Assert.Null(reset.UsedAtUtc);
    }
}
