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

    public string? ProtectedTotpSecret { get; set; }

    public string? PendingProtectedTotpSecret { get; set; }

    public DateTimeOffset? TotpEnrollmentStartedAtUtc { get; set; }

    public DateTimeOffset? TotpEnabledAtUtc { get; set; }

    public long? LastTotpStep { get; set; }

    public string[] RecoveryCodeHashes { get; set; } = [];

    public string[] PendingRecoveryCodeHashes { get; set; } = [];

    public DateTimeOffset? RecoveryCodesGeneratedAtUtc { get; set; }

    public DateTimeOffset? LastRecoveryCodeUsedAtUtc { get; set; }

    public int SecurityVersion { get; set; } = 1;

    public Guid ConcurrencyToken { get; set; }

    public Guid OriginalConcurrencyToken { get; set; }

    public bool IsTotpEnabled =>
        TotpEnabledAtUtc is not null && !string.IsNullOrWhiteSpace(ProtectedTotpSecret);

    public void RecordLogin(DateTimeOffset loggedInAtUtc)
    {
        LastLoginAtUtc = loggedInAtUtc;
        MarkChanged();
    }

    public void BeginTotpEnrollment(
        string protectedSecret,
        IReadOnlyCollection<string> recoveryCodeHashes,
        DateTimeOffset startedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(protectedSecret))
        {
            throw new InvalidOperationException("A protected TOTP secret is required.");
        }

        if (recoveryCodeHashes.Count == 0)
        {
            throw new InvalidOperationException("At least one recovery code is required.");
        }

        PendingProtectedTotpSecret = protectedSecret.Trim();
        TotpEnrollmentStartedAtUtc = startedAtUtc;
        PendingRecoveryCodeHashes = recoveryCodeHashes
            .Where(hash => !string.IsNullOrWhiteSpace(hash))
            .Select(hash => hash.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        MarkChanged();
    }

    public void ConfirmTotpEnrollment(long acceptedStep, DateTimeOffset confirmedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(PendingProtectedTotpSecret)
            || TotpEnrollmentStartedAtUtc is null)
        {
            throw new InvalidOperationException("A TOTP enrollment has not been started.");
        }

        ProtectedTotpSecret = PendingProtectedTotpSecret;
        PendingProtectedTotpSecret = null;
        TotpEnrollmentStartedAtUtc = null;
        RecoveryCodeHashes = PendingRecoveryCodeHashes;
        PendingRecoveryCodeHashes = [];
        RecoveryCodesGeneratedAtUtc = confirmedAtUtc;
        LastRecoveryCodeUsedAtUtc = null;
        TotpEnabledAtUtc = confirmedAtUtc;
        LastTotpStep = acceptedStep;
        SecurityVersion = checked(SecurityVersion + 1);
        MarkChanged();
    }

    public void RecordTotpUse(long acceptedStep)
    {
        if (LastTotpStep is not null && acceptedStep <= LastTotpStep.Value)
        {
            throw new InvalidOperationException("A TOTP step cannot be reused.");
        }

        LastTotpStep = acceptedStep;
        MarkChanged();
    }

    public bool ConsumeRecoveryCode(string recoveryCodeHash, DateTimeOffset usedAtUtc)
    {
        var matchingHash = RecoveryCodeHashes.FirstOrDefault(hash =>
            hash.Equals(recoveryCodeHash, StringComparison.Ordinal));

        if (matchingHash is null)
        {
            return false;
        }

        RecoveryCodeHashes = RecoveryCodeHashes
            .Where(hash => !hash.Equals(matchingHash, StringComparison.Ordinal))
            .ToArray();
        LastRecoveryCodeUsedAtUtc = usedAtUtc;
        MarkChanged();
        return true;
    }

    public void ChangePassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new InvalidOperationException("A password hash is required.");
        }

        PasswordHash = passwordHash;
        SecurityVersion = checked(SecurityVersion + 1);
        MarkChanged();
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
        var concurrencyToken = Guid.NewGuid();
        return new ControlCloudClientPortalUser
        {
            UserId = userId,
            ClientId = clientId,
            Email = ControlCloudClientPortalInvitation.NormalizeEmail(email),
            FullName = fullName.Trim(),
            Role = ControlCloudClientPortalInvitation.NormalizeRole(role),
            PasswordHash = passwordHash,
            Status = ControlCloudClientPortalUserStatuses.Active,
            CreatedAtUtc = createdAtUtc,
            SecurityVersion = 1,
            ConcurrencyToken = concurrencyToken,
            OriginalConcurrencyToken = concurrencyToken
        };
    }

    private void MarkChanged()
    {
        if (OriginalConcurrencyToken == Guid.Empty)
        {
            OriginalConcurrencyToken = ConcurrencyToken;
        }

        ConcurrencyToken = Guid.NewGuid();
    }
}

public static class ControlCloudClientPortalUserStatuses
{
    public const string Active = "Active";
    public const string Suspended = "Suspended";
}
