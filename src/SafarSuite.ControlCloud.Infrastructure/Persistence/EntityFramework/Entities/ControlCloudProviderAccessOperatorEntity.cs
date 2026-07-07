namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

public sealed class ControlCloudProviderAccessOperatorEntity
{
    public string UserId { get; set; } = "";

    public string Email { get; set; } = "";

    public string NormalizedEmail { get; set; } = "";

    public string FullName { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public string Status { get; set; } = "";

    public string ScopesJson { get; set; } = "";

    public string RecoveryCodeHashesJson { get; set; } = "";

    public DateTimeOffset? RecoveryCodesUpdatedAtUtc { get; set; }

    public string? RecoveryCodesUpdatedBy { get; set; }

    public DateTimeOffset? LastRecoveryCodeUsedAtUtc { get; set; }

    public string? TotpSecret { get; set; }

    public DateTimeOffset? TotpEnabledAtUtc { get; set; }

    public DateTimeOffset? TotpUpdatedAtUtc { get; set; }

    public string? TotpUpdatedBy { get; set; }

    public DateTimeOffset? LastTotpUsedAtUtc { get; set; }

    public long? LastTotpStep { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string CreatedBy { get; set; } = "";

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTimeOffset? LastLoginAtUtc { get; set; }
}
