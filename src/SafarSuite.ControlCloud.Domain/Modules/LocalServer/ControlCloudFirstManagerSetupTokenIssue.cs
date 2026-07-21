namespace SafarSuite.ControlCloud.Domain.Modules.LocalServer;

public sealed class ControlCloudFirstManagerSetupTokenIssue
{
    private ControlCloudFirstManagerSetupTokenIssue(
        Guid tokenId,
        Guid clientId,
        string installationId,
        Guid pendingDeviceRequestId,
        string managerDisplayName,
        string? managerEmail,
        string createdBy,
        string signingKeyId,
        string payloadSha256,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        TokenId = tokenId;
        ClientId = clientId;
        InstallationId = NormalizeRequiredText(installationId, nameof(installationId), 160);
        PendingDeviceRequestId = pendingDeviceRequestId;
        ManagerDisplayName = NormalizeRequiredText(managerDisplayName, nameof(managerDisplayName), 160);
        ManagerEmail = NormalizeOptionalText(managerEmail, 160);
        CreatedBy = NormalizeRequiredText(createdBy, nameof(createdBy), 160);
        SigningKeyId = NormalizeRequiredText(signingKeyId, nameof(signingKeyId), 120);
        PayloadSha256 = NormalizeRequiredText(payloadSha256, nameof(payloadSha256), 128);
        IssuedAtUtc = issuedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid TokenId { get; }

    public Guid ClientId { get; }

    public string InstallationId { get; }

    public Guid PendingDeviceRequestId { get; }

    public string ManagerDisplayName { get; }

    public string? ManagerEmail { get; }

    public string CreatedBy { get; }

    public string SigningKeyId { get; }

    public string PayloadSha256 { get; }

    public DateTimeOffset IssuedAtUtc { get; }

    public DateTimeOffset ExpiresAtUtc { get; }

    public static ControlCloudFirstManagerSetupTokenIssue Create(
        Guid tokenId,
        Guid clientId,
        string installationId,
        Guid pendingDeviceRequestId,
        string managerDisplayName,
        string? managerEmail,
        string createdBy,
        string signingKeyId,
        string payloadSha256,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (tokenId == Guid.Empty)
        {
            throw new ArgumentException("First-manager setup token id is required.", nameof(tokenId));
        }

        if (clientId == Guid.Empty)
        {
            throw new ArgumentException("Client id is required.", nameof(clientId));
        }

        if (pendingDeviceRequestId == Guid.Empty)
        {
            throw new ArgumentException("Pending device request id is required.", nameof(pendingDeviceRequestId));
        }

        if (expiresAtUtc <= issuedAtUtc)
        {
            throw new ArgumentException(
                "First-manager setup token expiry must be after issued time.",
                nameof(expiresAtUtc));
        }

        return new ControlCloudFirstManagerSetupTokenIssue(
            tokenId,
            clientId,
            installationId,
            pendingDeviceRequestId,
            managerDisplayName,
            managerEmail,
            createdBy,
            signingKeyId,
            payloadSha256,
            issuedAtUtc,
            expiresAtUtc);
    }

    public static ControlCloudFirstManagerSetupTokenIssue Restore(
        Guid tokenId,
        Guid clientId,
        string installationId,
        Guid pendingDeviceRequestId,
        string managerDisplayName,
        string? managerEmail,
        string createdBy,
        string signingKeyId,
        string payloadSha256,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        return new ControlCloudFirstManagerSetupTokenIssue(
            tokenId,
            clientId,
            installationId,
            pendingDeviceRequestId,
            managerDisplayName,
            managerEmail,
            createdBy,
            signingKeyId,
            payloadSha256,
            issuedAtUtc,
            expiresAtUtc);
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
