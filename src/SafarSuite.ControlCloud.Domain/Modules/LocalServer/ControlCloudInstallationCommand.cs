namespace SafarSuite.ControlCloud.Domain.Modules.LocalServer;

public sealed class ControlCloudInstallationCommand
{
    private ControlCloudInstallationCommand(
        Guid commandId,
        Guid clientId,
        string installationId,
        long commandVersion,
        string commandType,
        string status,
        string idempotencyKey,
        string payloadJson,
        string signatureAlgorithm,
        string signatureKeyId,
        string payloadSha256,
        string signatureValue,
        DateTimeOffset queuedAtUtc,
        DateTimeOffset? notBeforeUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? acknowledgedAtUtc,
        string? acknowledgementStatus,
        string? acknowledgementDetail)
    {
        CommandId = commandId;
        ClientId = clientId;
        InstallationId = NormalizeInstallationId(installationId);
        CommandVersion = commandVersion;
        CommandType = NormalizeRequiredText(commandType, nameof(commandType), 80);
        Status = NormalizeRequiredText(status, nameof(status), 32);
        IdempotencyKey = NormalizeRequiredText(idempotencyKey, nameof(idempotencyKey), 240);
        PayloadJson = string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson.Trim();
        SignatureAlgorithm = NormalizeRequiredText(signatureAlgorithm, nameof(signatureAlgorithm), 40);
        SignatureKeyId = NormalizeRequiredText(signatureKeyId, nameof(signatureKeyId), 120);
        PayloadSha256 = NormalizeRequiredText(payloadSha256, nameof(payloadSha256), 64);
        SignatureValue = NormalizeRequiredText(signatureValue, nameof(signatureValue), 512);
        QueuedAtUtc = queuedAtUtc;
        NotBeforeUtc = notBeforeUtc;
        ExpiresAtUtc = expiresAtUtc;
        AcknowledgedAtUtc = acknowledgedAtUtc;
        AcknowledgementStatus = NormalizeOptionalText(acknowledgementStatus, 32);
        AcknowledgementDetail = NormalizeOptionalText(acknowledgementDetail, 1000);
    }

    public Guid CommandId { get; }

    public Guid ClientId { get; }

    public string InstallationId { get; }

    public long CommandVersion { get; }

    public string CommandType { get; }

    public string Status { get; private set; }

    public string IdempotencyKey { get; }

    public string PayloadJson { get; }

    public string SignatureAlgorithm { get; }

    public string SignatureKeyId { get; }

    public string PayloadSha256 { get; }

    public string SignatureValue { get; }

    public DateTimeOffset QueuedAtUtc { get; }

    public DateTimeOffset? NotBeforeUtc { get; }

    public DateTimeOffset ExpiresAtUtc { get; }

    public DateTimeOffset? AcknowledgedAtUtc { get; private set; }

    public string? AcknowledgementStatus { get; private set; }

    public string? AcknowledgementDetail { get; private set; }

    public bool IsPending => Status == ControlCloudInstallationCommandStatuses.Pending;

    public static ControlCloudInstallationCommand Queue(
        Guid commandId,
        Guid clientId,
        string installationId,
        long commandVersion,
        string commandType,
        string idempotencyKey,
        string payloadJson,
        ControlCloudInstallationCommandSignature signature,
        DateTimeOffset queuedAtUtc,
        DateTimeOffset? notBeforeUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (commandId == Guid.Empty)
        {
            throw new ArgumentException("Command id is required.", nameof(commandId));
        }

        if (clientId == Guid.Empty)
        {
            throw new ArgumentException("Client id is required.", nameof(clientId));
        }

        if (commandVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(commandVersion),
                "Command version must be greater than zero.");
        }

        if (expiresAtUtc <= queuedAtUtc)
        {
            throw new ArgumentException(
                "Command expiry must be after the queued time.",
                nameof(expiresAtUtc));
        }

        return new ControlCloudInstallationCommand(
            commandId,
            clientId,
            installationId,
            commandVersion,
            commandType,
            ControlCloudInstallationCommandStatuses.Pending,
            idempotencyKey,
            payloadJson,
            signature.Algorithm,
            signature.KeyId,
            signature.PayloadSha256,
            signature.Value,
            queuedAtUtc,
            notBeforeUtc,
            expiresAtUtc,
            acknowledgedAtUtc: null,
            acknowledgementStatus: null,
            acknowledgementDetail: null);
    }

    public static ControlCloudInstallationCommand Restore(
        Guid commandId,
        Guid clientId,
        string installationId,
        long commandVersion,
        string commandType,
        string status,
        string idempotencyKey,
        string payloadJson,
        string signatureAlgorithm,
        string signatureKeyId,
        string payloadSha256,
        string signatureValue,
        DateTimeOffset queuedAtUtc,
        DateTimeOffset? notBeforeUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? acknowledgedAtUtc,
        string? acknowledgementStatus,
        string? acknowledgementDetail)
    {
        return new ControlCloudInstallationCommand(
            commandId,
            clientId,
            installationId,
            commandVersion,
            commandType,
            status,
            idempotencyKey,
            payloadJson,
            signatureAlgorithm,
            signatureKeyId,
            payloadSha256,
            signatureValue,
            queuedAtUtc,
            notBeforeUtc,
            expiresAtUtc,
            acknowledgedAtUtc,
            acknowledgementStatus,
            acknowledgementDetail);
    }

    public void Acknowledge(
        string resultStatus,
        string? detail,
        DateTimeOffset acknowledgedAtUtc)
    {
        if (!IsPending)
        {
            return;
        }

        var normalizedResult = NormalizeRequiredText(resultStatus, nameof(resultStatus), 32);

        AcknowledgedAtUtc = acknowledgedAtUtc;
        AcknowledgementStatus = normalizedResult;
        AcknowledgementDetail = NormalizeOptionalText(detail, 1000);
        Status = normalizedResult == ControlCloudInstallationCommandAcknowledgementStatuses.Applied
            ? ControlCloudInstallationCommandStatuses.Acknowledged
            : ControlCloudInstallationCommandStatuses.Failed;
    }

    private static string NormalizeInstallationId(string installationId)
    {
        return NormalizeRequiredText(installationId, nameof(installationId), 160);
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

public static class ControlCloudInstallationCommandStatuses
{
    public const string Pending = "Pending";
    public const string Acknowledged = "Acknowledged";
    public const string Failed = "Failed";
}

public static class ControlCloudInstallationCommandAcknowledgementStatuses
{
    public const string Applied = "Applied";
    public const string Failed = "Failed";
    public const string Rejected = "Rejected";
}

public sealed record ControlCloudInstallationCommandSigningPayload(
    Guid CommandId,
    Guid ClientId,
    string InstallationId,
    long CommandVersion,
    string CommandType,
    string PayloadJson,
    DateTimeOffset QueuedAtUtc,
    DateTimeOffset? NotBeforeUtc,
    DateTimeOffset ExpiresAtUtc);

public sealed record ControlCloudInstallationCommandSignature(
    string Algorithm,
    string KeyId,
    string PayloadSha256,
    string Value);
