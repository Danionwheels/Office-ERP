namespace SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

public static class LocalServerAppActivationRevocationStatusFormat
{
    public const string Version = "safarsuite-local-app-activation-revocation-status-v1";
}

public static class LocalServerAppActivationRevocationStates
{
    public const string NotRevoked = "NotRevoked";
    public const string Revoked = "Revoked";
    public const string RevokedIdentityMismatch = "RevokedIdentityMismatch";
}

public sealed record LocalServerAppActivationRevocationStatusRequest(
    Guid ClientId,
    string InstallationId,
    Guid AppServerInstallationId,
    Guid ActivationIssueId,
    string? FingerprintHash = null,
    string? ServerPublicKeySha256 = null,
    string? RequestedBy = null);

public sealed record LocalServerAppActivationRevocationStatusResponse(
    string FormatVersion,
    Guid ClientId,
    string InstallationId,
    Guid AppServerInstallationId,
    Guid ActivationIssueId,
    bool IsRevoked,
    bool IdentityMatched,
    string RevocationState,
    string Reason,
    DateTimeOffset CheckedAtUtc,
    DateTimeOffset? RevokedAtUtc,
    DateTimeOffset? RecordedAtUtc,
    Guid? ActivationRequestId,
    string? RevokedBy,
    string? SigningKeyId,
    long? CommandVersion);
