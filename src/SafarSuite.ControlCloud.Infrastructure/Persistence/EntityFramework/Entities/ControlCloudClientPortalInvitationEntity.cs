namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

public sealed class ControlCloudClientPortalInvitationEntity
{
    public Guid InvitationId { get; set; }

    public Guid ClientId { get; set; }

    public string Email { get; set; } = "";

    public string NormalizedEmail { get; set; } = "";

    public string FullName { get; set; } = "";

    public string Role { get; set; } = "";

    public string TokenHash { get; set; } = "";

    public string Status { get; set; } = "";

    public string CreatedBy { get; set; } = "";

    public DateTimeOffset InvitedAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? AcceptedAtUtc { get; set; }

    public Guid? AcceptedUserId { get; set; }
}
