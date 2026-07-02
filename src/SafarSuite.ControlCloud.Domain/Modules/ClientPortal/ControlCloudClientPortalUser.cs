namespace SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

public sealed class ControlCloudClientPortalUser
{
    public Guid UserId { get; set; }

    public Guid ClientId { get; set; }

    public string Email { get; set; } = "";

    public string FullName { get; set; } = "";

    public string Role { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public string Status { get; set; } = ControlCloudClientPortalUserStatuses.Active;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? LastLoginAtUtc { get; set; }

    public void RecordLogin(DateTimeOffset loggedInAtUtc)
    {
        LastLoginAtUtc = loggedInAtUtc;
    }

    public static ControlCloudClientPortalUser Create(
        Guid userId,
        Guid clientId,
        string email,
        string fullName,
        string role,
        string passwordHash,
        DateTimeOffset createdAtUtc)
    {
        return new ControlCloudClientPortalUser
        {
            UserId = userId,
            ClientId = clientId,
            Email = ControlCloudClientPortalInvitation.NormalizeEmail(email),
            FullName = fullName.Trim(),
            Role = ControlCloudClientPortalInvitation.NormalizeRole(role),
            PasswordHash = passwordHash,
            Status = ControlCloudClientPortalUserStatuses.Active,
            CreatedAtUtc = createdAtUtc
        };
    }
}

public static class ControlCloudClientPortalUserStatuses
{
    public const string Active = "Active";
    public const string Suspended = "Suspended";
}
