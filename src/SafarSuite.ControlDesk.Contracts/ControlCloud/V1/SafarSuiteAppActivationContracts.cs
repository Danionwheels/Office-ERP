namespace SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

public sealed record SafarSuiteAppActivationSigningKeyResponse(
    string SigningKeyId,
    string PublicKeyPem);

public sealed record IssueSafarSuiteAppActivationTokenRequest(
    Guid? ActivationRequestId,
    Guid ServerInstallationId,
    string FingerprintHash,
    string ServerPublicKey,
    string? RequestedBy = null,
    Guid? ReplacesActivationIssueId = null);

public sealed record SafarSuiteAppActivationTokenImportResponse(
    string ActivationToken,
    string Signature,
    string SigningKeyId,
    Guid TenantId,
    Guid BranchId,
    string CustomerCode,
    string CustomerName,
    string BranchName,
    DateOnly PaidUntil,
    DateOnly GraceEndsOn,
    DateOnly OfflineValidUntil,
    IReadOnlyDictionary<string, bool> ModuleEntitlements);

public sealed record IssueSafarSuiteAppActivationTokenResponse(
    Guid ActivationIssueId,
    Guid ClientId,
    string InstallationId,
    Guid AppServerInstallationId,
    Guid ActivationRequestId,
    Guid? ReplacesActivationIssueId,
    long EntitlementVersion,
    string SigningKeyId,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    SafarSuiteAppActivationTokenImportResponse Import);

public sealed record RevokeSafarSuiteAppActivationIssueRequest(
    string RevokedBy,
    string Reason);

public sealed record SafarSuiteAppActivationIssueResponse(
    Guid ActivationIssueId,
    Guid ClientId,
    string InstallationId,
    Guid AppServerInstallationId,
    Guid ActivationRequestId,
    Guid? ReplacesActivationIssueId,
    string FingerprintHash,
    string ServerPublicKeySha256,
    long EntitlementVersion,
    string SigningKeyId,
    string Status,
    string RequestedBy,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? RevokedAtUtc,
    string? RevokedBy,
    string? RevocationReason);

public sealed record SafarSuiteAppActivationIssuesResponse(
    IReadOnlyCollection<SafarSuiteAppActivationIssueResponse> Issues);
