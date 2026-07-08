namespace SafarSuite.LocalServer.Domain.Pairing;

public sealed record LocalServerDevicePairingRecord(
    Guid PairingRequestId,
    Guid DeviceId,
    Guid ClientId,
    string InstallationId,
    string RequestFormatVersion,
    string DeviceDisplayName,
    string DevicePublicKey,
    string? DevicePublicKeySha256,
    string? DeviceFingerprintHash,
    string? WindowsUserHint,
    string? AppVersion,
    string? HelloServerNonce,
    string? HelloClientNonce,
    string PairingRequestStatus,
    string DeviceStatus,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? ApprovedAtUtc = null,
    string? ApprovedBy = null,
    string? AssignedRole = null,
    string? DeviceCredentialId = null,
    string? DeviceCredentialSha256 = null,
    DateTimeOffset? DeviceCredentialIssuedAtUtc = null,
    DateTimeOffset? SuspendedAtUtc = null,
    string? SuspendedBy = null,
    string? SuspensionReason = null,
    DateTimeOffset? RevokedAtUtc = null,
    string? RevokedBy = null,
    string? RevocationReason = null)
{
    public LocalServerDevicePairingRecord Approve(
        string approvedBy,
        string assignedRole,
        string deviceCredentialId,
        string deviceCredentialSha256,
        DateTimeOffset approvedAtUtc)
    {
        return this with
        {
            PairingRequestStatus = LocalServerDevicePairingRecordStatuses.Approved,
            DeviceStatus = LocalServerDevicePairingRecordStatuses.Approved,
            ApprovedAtUtc = approvedAtUtc,
            ApprovedBy = NormalizeOptional(approvedBy, 160),
            AssignedRole = NormalizeOptional(assignedRole, 80),
            DeviceCredentialId = NormalizeOptional(deviceCredentialId, 120),
            DeviceCredentialSha256 = NormalizeOptional(deviceCredentialSha256, 160),
            DeviceCredentialIssuedAtUtc = approvedAtUtc,
            SuspendedAtUtc = null,
            SuspendedBy = null,
            SuspensionReason = null,
            UpdatedAtUtc = approvedAtUtc
        };
    }

    public LocalServerDevicePairingRecord Suspend(
        string suspendedBy,
        string reason,
        DateTimeOffset suspendedAtUtc)
    {
        return this with
        {
            DeviceStatus = LocalServerDevicePairingRecordStatuses.Suspended,
            SuspendedAtUtc = suspendedAtUtc,
            SuspendedBy = NormalizeOptional(suspendedBy, 160),
            SuspensionReason = NormalizeOptional(reason, 500),
            UpdatedAtUtc = suspendedAtUtc
        };
    }

    public LocalServerDevicePairingRecord Revoke(
        string revokedBy,
        string reason,
        DateTimeOffset revokedAtUtc)
    {
        return this with
        {
            PairingRequestStatus = LocalServerDevicePairingRecordStatuses.Revoked,
            DeviceStatus = LocalServerDevicePairingRecordStatuses.Revoked,
            RevokedAtUtc = revokedAtUtc,
            RevokedBy = NormalizeOptional(revokedBy, 160),
            RevocationReason = NormalizeOptional(reason, 500),
            UpdatedAtUtc = revokedAtUtc
        };
    }

    public bool IsActiveRequestForPublicKey(
        string installationId,
        string devicePublicKeySha256)
    {
        return string.Equals(InstallationId, installationId, StringComparison.Ordinal)
            && string.Equals(DevicePublicKeySha256, devicePublicKeySha256, StringComparison.Ordinal)
            && PairingRequestStatus is not LocalServerDevicePairingRecordStatuses.Revoked
            && DeviceStatus is not LocalServerDevicePairingRecordStatuses.Revoked;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }
}

public static class LocalServerDevicePairingRecordStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Suspended = "Suspended";
    public const string Revoked = "Revoked";
    public const string Blocked = "Blocked";
    public const string Retired = "Retired";
}
