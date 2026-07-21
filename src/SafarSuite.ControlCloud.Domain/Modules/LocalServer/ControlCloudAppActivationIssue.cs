namespace SafarSuite.ControlCloud.Domain.Modules.LocalServer;

public sealed class ControlCloudAppActivationIssue
{
    private ControlCloudAppActivationIssue(
        Guid activationIssueId,
        Guid clientId,
        string installationId,
        Guid appServerInstallationId,
        Guid activationRequestId,
        Guid? replacesActivationIssueId,
        string fingerprintHash,
        string serverPublicKeySha256,
        long entitlementVersion,
        string signingKeyId,
        string status,
        string requestedBy,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? revokedAtUtc,
        string? revokedBy,
        string? revocationReason)
    {
        ActivationIssueId = activationIssueId;
        ClientId = clientId;
        InstallationId = NormalizeRequiredText(installationId, nameof(installationId), 160);
        AppServerInstallationId = appServerInstallationId;
        ActivationRequestId = activationRequestId;
        ReplacesActivationIssueId = replacesActivationIssueId;
        FingerprintHash = NormalizeRequiredText(fingerprintHash, nameof(fingerprintHash), 512);
        ServerPublicKeySha256 = NormalizeRequiredText(serverPublicKeySha256, nameof(serverPublicKeySha256), 128);
        EntitlementVersion = entitlementVersion;
        SigningKeyId = NormalizeRequiredText(signingKeyId, nameof(signingKeyId), 120);
        Status = NormalizeRequiredText(status, nameof(status), 32);
        RequestedBy = NormalizeRequiredText(requestedBy, nameof(requestedBy), 120);
        IssuedAtUtc = issuedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        RevokedAtUtc = revokedAtUtc;
        RevokedBy = NormalizeOptionalText(revokedBy, 120);
        RevocationReason = NormalizeOptionalText(revocationReason, 500);
    }

    public Guid ActivationIssueId { get; }

    public Guid ClientId { get; }

    public string InstallationId { get; }

    public Guid AppServerInstallationId { get; }

    public Guid ActivationRequestId { get; }

    public Guid? ReplacesActivationIssueId { get; }

    public string FingerprintHash { get; }

    public string ServerPublicKeySha256 { get; }

    public long EntitlementVersion { get; }

    public string SigningKeyId { get; }

    public string Status { get; private set; }

    public string RequestedBy { get; }

    public DateTimeOffset IssuedAtUtc { get; }

    public DateTimeOffset ExpiresAtUtc { get; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public string? RevokedBy { get; private set; }

    public string? RevocationReason { get; private set; }

    public static ControlCloudAppActivationIssue Create(
        Guid activationIssueId,
        Guid clientId,
        string installationId,
        Guid appServerInstallationId,
        Guid activationRequestId,
        Guid? replacesActivationIssueId,
        string fingerprintHash,
        string serverPublicKeySha256,
        long entitlementVersion,
        string signingKeyId,
        string requestedBy,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (activationIssueId == Guid.Empty)
        {
            throw new ArgumentException("Activation issue id is required.", nameof(activationIssueId));
        }

        if (clientId == Guid.Empty)
        {
            throw new ArgumentException("Client id is required.", nameof(clientId));
        }

        if (appServerInstallationId == Guid.Empty)
        {
            throw new ArgumentException("App server installation id is required.", nameof(appServerInstallationId));
        }

        if (activationRequestId == Guid.Empty)
        {
            throw new ArgumentException("Activation request id is required.", nameof(activationRequestId));
        }

        if (replacesActivationIssueId == Guid.Empty)
        {
            throw new ArgumentException(
                "Replacement activation issue id cannot be empty.",
                nameof(replacesActivationIssueId));
        }

        if (expiresAtUtc <= issuedAtUtc)
        {
            throw new ArgumentException(
                "Activation issue expiry must be after issued time.",
                nameof(expiresAtUtc));
        }

        return new ControlCloudAppActivationIssue(
            activationIssueId,
            clientId,
            installationId,
            appServerInstallationId,
            activationRequestId,
            replacesActivationIssueId,
            fingerprintHash,
            serverPublicKeySha256,
            entitlementVersion,
            signingKeyId,
            ControlCloudAppActivationIssueStatuses.Issued,
            requestedBy,
            issuedAtUtc,
            expiresAtUtc,
            revokedAtUtc: null,
            revokedBy: null,
            revocationReason: null);
    }

    public static ControlCloudAppActivationIssue Restore(
        Guid activationIssueId,
        Guid clientId,
        string installationId,
        Guid appServerInstallationId,
        Guid activationRequestId,
        Guid? replacesActivationIssueId,
        string fingerprintHash,
        string serverPublicKeySha256,
        long entitlementVersion,
        string signingKeyId,
        string status,
        string requestedBy,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? revokedAtUtc,
        string? revokedBy,
        string? revocationReason)
    {
        return new ControlCloudAppActivationIssue(
            activationIssueId,
            clientId,
            installationId,
            appServerInstallationId,
            activationRequestId,
            replacesActivationIssueId,
            fingerprintHash,
            serverPublicKeySha256,
            entitlementVersion,
            signingKeyId,
            status,
            requestedBy,
            issuedAtUtc,
            expiresAtUtc,
            revokedAtUtc,
            revokedBy,
            revocationReason);
    }

    public void Revoke(
        string revokedBy,
        string reason,
        DateTimeOffset revokedAtUtc)
    {
        if (Status == ControlCloudAppActivationIssueStatuses.Revoked)
        {
            throw new InvalidOperationException("App activation issue is already revoked.");
        }

        Status = ControlCloudAppActivationIssueStatuses.Revoked;
        RevokedAtUtc = revokedAtUtc;
        RevokedBy = NormalizeRequiredText(revokedBy, nameof(revokedBy), 120);
        RevocationReason = NormalizeRequiredText(reason, nameof(reason), 500);
    }

    private static string NormalizeRequiredText(
        string value,
        string parameterName,
        int maxLength)
    {
        var normalized = value.Trim();

        if (normalized.Length == 0)
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }

    private static string? NormalizeOptionalText(string? value, int maxLength)
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

public static class ControlCloudAppActivationIssueStatuses
{
    public const string Issued = "Issued";
    public const string Revoked = "Revoked";
}
