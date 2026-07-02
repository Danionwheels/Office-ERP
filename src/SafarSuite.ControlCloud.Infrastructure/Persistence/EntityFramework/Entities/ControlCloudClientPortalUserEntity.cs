namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

public sealed class ControlCloudClientPortalUserEntity
{
    public Guid UserId { get; set; }

    public Guid ClientId { get; set; }

    public string Email { get; set; } = "";

    public string NormalizedEmail { get; set; } = "";

    public string FullName { get; set; } = "";

    public string Role { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public string Status { get; set; } = "";

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? LastLoginAtUtc { get; set; }
}
