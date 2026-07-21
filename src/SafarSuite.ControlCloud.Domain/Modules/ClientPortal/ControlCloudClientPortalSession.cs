namespace SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

public sealed class ControlCloudClientPortalSession
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public Guid ClientId { get; set; }
    public string Role { get; set; } = "";
    public int SecurityVersion { get; set; }
    public string RefreshTokenHash { get; set; } = "";
    public string? PreviousRefreshTokenHash { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset LastActivityAtUtc { get; set; }
    public DateTimeOffset IdleExpiresAtUtc { get; set; }
    public DateTimeOffset AbsoluteExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public string? RevokedReason { get; set; }
    public Guid ConcurrencyToken { get; set; }
    public Guid OriginalConcurrencyToken { get; set; }

    public bool IsActiveAt(DateTimeOffset now, int currentSecurityVersion) =>
        RevokedAtUtc is null
        && SecurityVersion == currentSecurityVersion
        && IdleExpiresAtUtc > now
        && AbsoluteExpiresAtUtc > now;

    public void Touch(DateTimeOffset now, TimeSpan idleTimeout)
    {
        if (RevokedAtUtc is not null)
        {
            return;
        }

        LastActivityAtUtc = now;
        IdleExpiresAtUtc = Min(now.Add(idleTimeout), AbsoluteExpiresAtUtc);
        MarkChanged();
    }

    public void RotateRefreshToken(string newRefreshTokenHash, DateTimeOffset now, TimeSpan idleTimeout)
    {
        PreviousRefreshTokenHash = RefreshTokenHash;
        RefreshTokenHash = newRefreshTokenHash;
        Touch(now, idleTimeout);
    }

    public void Revoke(DateTimeOffset now, string reason)
    {
        if (RevokedAtUtc is not null)
        {
            return;
        }

        RevokedAtUtc = now;
        RevokedReason = string.IsNullOrWhiteSpace(reason) ? "Revoked" : reason.Trim();
        MarkChanged();
    }

    public static ControlCloudClientPortalSession Create(
        Guid sessionId,
        Guid userId,
        Guid clientId,
        string role,
        int securityVersion,
        string refreshTokenHash,
        DateTimeOffset now,
        TimeSpan idleTimeout,
        TimeSpan absoluteTimeout)
    {
        var concurrencyToken = Guid.NewGuid();
        return new ControlCloudClientPortalSession
        {
            SessionId = sessionId,
            UserId = userId,
            ClientId = clientId,
            Role = string.IsNullOrWhiteSpace(role) ? "ClientViewer" : role.Trim(),
            SecurityVersion = securityVersion,
            RefreshTokenHash = refreshTokenHash,
            CreatedAtUtc = now,
            LastActivityAtUtc = now,
            IdleExpiresAtUtc = Min(now.Add(idleTimeout), now.Add(absoluteTimeout)),
            AbsoluteExpiresAtUtc = now.Add(absoluteTimeout),
            ConcurrencyToken = concurrencyToken,
            OriginalConcurrencyToken = concurrencyToken
        };
    }

    private static DateTimeOffset Min(DateTimeOffset left, DateTimeOffset right) =>
        left <= right ? left : right;

    private void MarkChanged()
    {
        if (OriginalConcurrencyToken == Guid.Empty)
        {
            OriginalConcurrencyToken = ConcurrencyToken;
        }

        ConcurrencyToken = Guid.NewGuid();
    }
}
