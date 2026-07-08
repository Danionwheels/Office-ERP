namespace SafarSuite.LocalServer.Domain.Pairing;

public sealed record LocalServerFirstManagerSetupTokenConsumptionRecord(
    Guid TokenId,
    Guid ClientId,
    string InstallationId,
    Guid PairingRequestId,
    Guid DeviceId,
    string ManagerDisplayName,
    string? ManagerEmail,
    string CreatedBy,
    string SignatureKeyId,
    string PayloadSha256,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset ConsumedAtUtc);
