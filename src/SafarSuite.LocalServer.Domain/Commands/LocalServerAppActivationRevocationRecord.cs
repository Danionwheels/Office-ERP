namespace SafarSuite.LocalServer.Domain.Commands;

public sealed record LocalServerAppActivationRevocationRecord(
    Guid ActivationIssueId,
    Guid ClientId,
    string InstallationId,
    Guid AppServerInstallationId,
    Guid ActivationRequestId,
    string FingerprintHash,
    string ServerPublicKeySha256,
    string SigningKeyId,
    DateTimeOffset RevokedAtUtc,
    string RevokedBy,
    string Reason,
    Guid CommandId,
    long CommandVersion,
    DateTimeOffset RecordedAtUtc);
