namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

public sealed class ControlCloudClientPortalPasswordResetEntity
{
    public Guid PasswordResetId { get; set; }
    public Guid UserId { get; set; }
    public Guid ClientId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? UsedAtUtc { get; set; }
    public Guid ConcurrencyToken { get; set; }
}
