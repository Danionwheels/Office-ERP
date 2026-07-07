namespace SafarSuite.ControlCloud.Api.Modules.ProviderAccess;

public sealed class ProviderAccessOperator
{
    public string UserId { get; set; } = "";

    public string Email { get; set; } = "";

    public string FullName { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public string Status { get; set; } = ProviderAccessOperatorStatuses.Active;

    public string[] Scopes { get; set; } = [];

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string CreatedBy { get; set; } = "";

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTimeOffset? LastLoginAtUtc { get; set; }
}

public static class ProviderAccessOperatorStatuses
{
    public const string Active = "Active";
    public const string Suspended = "Suspended";

    public static bool IsSupported(string? status)
    {
        return status is not null
            && (status.Equals(Active, StringComparison.Ordinal)
                || status.Equals(Suspended, StringComparison.Ordinal));
    }
}
