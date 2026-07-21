namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

public sealed class ControlCloudClientPortalSessionEntity
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
}
