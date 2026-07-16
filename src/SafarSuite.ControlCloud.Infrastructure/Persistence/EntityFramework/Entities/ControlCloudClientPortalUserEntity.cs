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

    public string? ProtectedTotpSecret { get; set; }
    public string? PendingProtectedTotpSecret { get; set; }
    public DateTimeOffset? TotpEnrollmentStartedAtUtc { get; set; }
    public DateTimeOffset? TotpEnabledAtUtc { get; set; }
    public long? LastTotpStep { get; set; }
    public string RecoveryCodeHashesJson { get; set; } = "[]";
    public string PendingRecoveryCodeHashesJson { get; set; } = "[]";
    public DateTimeOffset? RecoveryCodesGeneratedAtUtc { get; set; }
    public DateTimeOffset? LastRecoveryCodeUsedAtUtc { get; set; }
    public int SecurityVersion { get; set; } = 1;
    public Guid ConcurrencyToken { get; set; }
}
