namespace SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

public sealed class ControlCloudClientPortalPasswordReset
{
    public Guid PasswordResetId { get; set; }
    public Guid UserId { get; set; }
    public Guid ClientId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? UsedAtUtc { get; set; }
    public Guid ConcurrencyToken { get; set; }
    public Guid OriginalConcurrencyToken { get; set; }

    public bool IsUsableAt(DateTimeOffset now) => UsedAtUtc is null && ExpiresAtUtc > now;

    public bool TryConsume(DateTimeOffset now)
    {
        if (!IsUsableAt(now))
        {
            return false;
        }

        UsedAtUtc = now;
        OriginalConcurrencyToken = ConcurrencyToken;
        ConcurrencyToken = Guid.NewGuid();
        return true;
    }

    public static ControlCloudClientPortalPasswordReset Create(
        Guid passwordResetId,
        Guid userId,
        Guid clientId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (expiresAtUtc <= createdAtUtc)
        {
            throw new InvalidOperationException("Password reset expiry must be after creation.");
        }

        var concurrencyToken = Guid.NewGuid();
        return new ControlCloudClientPortalPasswordReset
        {
            PasswordResetId = passwordResetId,
            UserId = userId,
            ClientId = clientId,
            TokenHash = tokenHash,
            CreatedAtUtc = createdAtUtc,
            ExpiresAtUtc = expiresAtUtc,
            ConcurrencyToken = concurrencyToken,
            OriginalConcurrencyToken = concurrencyToken
        };
    }
}
