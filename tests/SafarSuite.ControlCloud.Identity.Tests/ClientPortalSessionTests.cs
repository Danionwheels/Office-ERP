using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Identity.Tests;

public sealed class ClientPortalSessionTests
{
    [Fact]
    public void IdleExpiryCanBeExtendedButNeverBeyondAbsoluteExpiry()
    {
        var now = new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero);
        var session = CreateSession(now);

        Assert.True(session.IsActiveAt(now.AddMinutes(29), currentSecurityVersion: 4));
        Assert.False(session.IsActiveAt(now.AddMinutes(30), currentSecurityVersion: 4));

        session.Touch(now.AddMinutes(20), TimeSpan.FromMinutes(30));

        Assert.Equal(now.AddMinutes(50), session.IdleExpiresAtUtc);
        Assert.True(session.IsActiveAt(now.AddMinutes(49), currentSecurityVersion: 4));

        session.Touch(now.AddHours(7).AddMinutes(50), TimeSpan.FromMinutes(30));

        Assert.Equal(now.AddHours(8), session.IdleExpiresAtUtc);
    }

    [Fact]
    public void RefreshRotationTracksThePreviousHashAndTouchesTheSession()
    {
        var now = new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero);
        var session = CreateSession(now);
        var originalConcurrencyToken = session.ConcurrencyToken;

        session.RotateRefreshToken(
            "refresh-hash-v2",
            now.AddMinutes(10),
            TimeSpan.FromMinutes(30));

        Assert.Equal("refresh-hash-v1", session.PreviousRefreshTokenHash);
        Assert.Equal("refresh-hash-v2", session.RefreshTokenHash);
        Assert.Equal(now.AddMinutes(10), session.LastActivityAtUtc);
        Assert.Equal(now.AddMinutes(40), session.IdleExpiresAtUtc);
        Assert.NotEqual(originalConcurrencyToken, session.ConcurrencyToken);
    }

    [Fact]
    public void RevokedSessionStaysInactiveAndCannotBeTouchedBackToLife()
    {
        var now = new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero);
        var session = CreateSession(now);

        session.Revoke(now.AddMinutes(5), "Forced logout");
        var lastActivity = session.LastActivityAtUtc;
        session.Touch(now.AddMinutes(6), TimeSpan.FromMinutes(30));

        Assert.False(session.IsActiveAt(now.AddMinutes(6), currentSecurityVersion: 4));
        Assert.Equal(now.AddMinutes(5), session.RevokedAtUtc);
        Assert.Equal("Forced logout", session.RevokedReason);
        Assert.Equal(lastActivity, session.LastActivityAtUtc);
    }

    private static ControlCloudClientPortalSession CreateSession(DateTimeOffset now)
    {
        return ControlCloudClientPortalSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ClientOwner",
            securityVersion: 4,
            "refresh-hash-v1",
            now,
            TimeSpan.FromMinutes(30),
            TimeSpan.FromHours(8));
    }
}
