namespace SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

public static class LocalServerPairingFormats
{
    public const string DiscoveryVersion = "safarsuite-local-discovery-v1";
    public const string HelloRequestVersion = "safarsuite-local-pairing-hello-request-v1";
    public const string HelloResponseVersion = "safarsuite-local-pairing-hello-v1";
    public const string DevicePairingRequestVersion = "safarsuite-local-device-pairing-request-v1";
    public const string DeviceCredentialVersion = "safarsuite-local-device-credential-v1";
    public const string PairingProfileVersion = "safarsuite-local-pairing-profile-v1";
    public const string PairingDescriptorVersion = "safarsuite-local-pairing-descriptor-v1";
    public const string PairingDirectoryVersion = "safarsuite-local-pairing-directory-v1";
    public const string FirstManagerSetupTokenVersion = "safarsuite-first-manager-setup-token-v1";
    public const string ManagerSessionVersion = "safarsuite-local-manager-session-v1";
}

public static class LocalServerDeviceCredentialHeaders
{
    public const string DeviceCredential = "X-SafarSuite-Device-Credential";
}

public static class LocalServerPairingModes
{
    public const string ManagerApproval = "ManagerApproval";
    public const string FirstManagerSetupRequired = "FirstManagerSetupRequired";
    public const string PairingDisabled = "PairingDisabled";
}

public static class LocalServerDevicePairingStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Suspended = "Suspended";
    public const string Revoked = "Revoked";
    public const string Blocked = "Blocked";
    public const string Retired = "Retired";
}

public static class LocalServerPairingAbuseEndpointGroups
{
    public const string Discovery = "Discovery";
    public const string PairingHello = "PairingHello";
    public const string PairingRequest = "PairingRequest";
    public const string PairingStatus = "PairingStatus";
    public const string FirstManagerTokenImport = "FirstManagerTokenImport";
    public const string DeviceCredential = "DeviceCredential";
    public const string ManagerSession = "ManagerSession";
}

public static class LocalServerPairingSecurityEventTypes
{
    public const string RateLimited = "RateLimited";
    public const string DuplicateCoalesced = "DuplicateCoalesced";
    public const string PendingQueueFull = "PendingQueueFull";
    public const string RequestTooLarge = "RequestTooLarge";
    public const string CredentialRejected = "CredentialRejected";
    public const string FirstManagerTokenRejected = "FirstManagerTokenRejected";
    public const string SourceQuarantined = "SourceQuarantined";
    public const string SourceDenied = "SourceDenied";
    public const string SourceAllowed = "SourceAllowed";
}

public static class LocalServerPairingSecuritySeverities
{
    public const string Information = "Information";
    public const string Warning = "Warning";
    public const string Error = "Error";
}

public static class LocalServerPairingAbuseActions
{
    public const string Allow = "Allow";
    public const string Coalesce = "Coalesce";
    public const string Reject = "Reject";
    public const string Throttle = "Throttle";
    public const string Quarantine = "Quarantine";
    public const string Deny = "Deny";
    public const string Release = "Release";
}

public static class LocalServerFirstManagerSetupTokenActions
{
    public const string CreateFirstManager = "CreateFirstManager";
    public const string ApproveFirstDevice = "ApproveFirstDevice";
    public const string RecoverManagerAccess = "RecoverManagerAccess";
    public const string ApproveManagerDevice = "ApproveManagerDevice";
}

public static class LocalServerFirstManagerSetupTokenPurposes
{
    public const string FirstManagerBootstrap = "FirstManagerBootstrap";
    public const string ManagerRecovery = "ManagerRecovery";
}

public sealed record LocalServerPairingStatusResponse(
    string PairingMode,
    int TotalDeviceCount,
    int PendingDeviceCount,
    int ApprovedDeviceCount,
    int SuspendedDeviceCount,
    int RevokedDeviceCount,
    bool FirstManagerDeviceApproved,
    DateTimeOffset? FirstManagerDeviceApprovedAtUtc,
    DateTimeOffset? LastDeviceUpdatedAtUtc);

public sealed record LocalServerPairingDiscoveryResponse(
    string FormatVersion,
    bool HasBootstrapConfiguration,
    Guid? ClientId,
    string? InstallationIdHint,
    string DisplayName,
    string PairingMode,
    LocalServerDeploymentProfileResponse? DeploymentProfile,
    IReadOnlyCollection<string> UrlCandidates,
    string? TlsCertificateSha256,
    string? TlsCaSha256,
    string? ServerPairingKeySha256,
    string? BootstrapPayloadSha256,
    string? BootstrapSignatureKeyId,
    DateTimeOffset GeneratedAtUtc);

public sealed record LocalServerPairingDescriptorResponse(
    string FormatVersion,
    Guid ClientId,
    string ProviderInstallationId,
    Guid? BootstrapPackageId,
    Guid? SetupTokenId,
    string DisplayName,
    string? AppServerInstallationId,
    string? SiteId,
    string? SiteRole,
    string? CustomerCode,
    string? CustomerName,
    string? BranchName,
    string? FingerprintHash,
    string? TlsCaSha256,
    string? TlsCertificateSha256,
    string? ServerPairingKeySha256,
    IReadOnlyCollection<string> UrlCandidates,
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    string? Source,
    string? BootstrapBundleSha256 = null,
    string? BootstrapSignatureKeyId = null,
    IReadOnlyCollection<string>? Notes = null,
    string? SignatureAlgorithm = null,
    string? SignatureKeyId = null,
    string? PayloadSha256 = null,
    string? Signature = null);

public sealed record IssueLocalServerPairingDescriptorRequest(
    Guid? BootstrapPackageId = null,
    Guid? SetupTokenId = null,
    string? ClientCode = null,
    string? CustomerName = null,
    string? AppServerInstallationId = null,
    string? FingerprintHash = null,
    IReadOnlyCollection<string>? UrlCandidates = null,
    string? TlsCaSha256 = null,
    string? TlsCertificateSha256 = null,
    string? ServerPairingKeySha256 = null,
    string? RequestedBy = null);

public sealed record LocalServerPairingHelloRequest(
    string FormatVersion,
    string ClientNonce,
    string? AppVersion = null,
    string? RequestedBy = null);

public sealed record LocalServerPairingHelloResponse(
    string FormatVersion,
    Guid ClientId,
    string InstallationId,
    Guid BootstrapPackageId,
    LocalServerDeploymentProfileResponse DeploymentProfile,
    string DisplayName,
    string LocalServerVersion,
    string PairingMode,
    IReadOnlyCollection<string> UrlCandidates,
    string? TlsCertificateSha256,
    string? TlsCaSha256,
    string? ServerPairingPublicKey,
    string? ServerPairingKeySha256,
    string BootstrapPayloadSha256,
    string BootstrapSignatureAlgorithm,
    string BootstrapSignatureKeyId,
    long? EntitlementVersion,
    DateOnly? PaidUntil,
    DateOnly? OfflineValidUntil,
    string ClientNonce,
    string ServerNonce,
    string? AppVersion,
    string? RequestedBy,
    DateTimeOffset GeneratedAtUtc);

public sealed record LocalServerDevicePairingRequest(
    string FormatVersion,
    string InstallationId,
    string DeviceDisplayName,
    string DevicePublicKey,
    string? DeviceFingerprintHash = null,
    string? WindowsUserHint = null,
    string? AppVersion = null,
    string? HelloServerNonce = null,
    string? HelloClientNonce = null,
    DateTimeOffset? RequestedAtUtc = null);

public sealed record LocalServerDevicePairingRequestResponse(
    Guid PairingRequestId,
    Guid DeviceId,
    Guid ClientId,
    string InstallationId,
    string PairingRequestStatus,
    string DeviceStatus,
    string DeviceDisplayName,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    bool Coalesced = false);

public sealed record LocalServerDevicePairingRequestsResponse(
    IReadOnlyCollection<LocalServerDeviceResponse> Devices);

public sealed record LocalServerDeviceRegisterResponse(
    IReadOnlyCollection<LocalServerDeviceResponse> Devices);

public sealed record LocalServerPairingAbuseProblemResponse(
    string Code,
    string Detail,
    Guid EventId,
    string LimitScope,
    string EndpointGroup,
    int? RetryAfterSeconds,
    DateTimeOffset? WindowStartedAtUtc,
    DateTimeOffset? WindowExpiresAtUtc);

public sealed record LocalServerPairingSecurityEventResponse(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    string EventType,
    string Severity,
    string EndpointGroup,
    string SourceKey,
    string? RemoteAddress,
    string? DeviceInstallIdHash,
    string? DeviceFingerprintHash,
    Guid? PairingRequestId,
    Guid? DeviceId,
    int Count,
    DateTimeOffset? WindowStartedAtUtc,
    DateTimeOffset? WindowExpiresAtUtc,
    string Action,
    string Detail);

public sealed record LocalServerPairingSecurityEventsResponse(
    IReadOnlyCollection<LocalServerPairingSecurityEventResponse> Events);

public sealed record LocalServerPairingAbuseSourceDecisionResponse(
    string SourceKey,
    string Action,
    string Reason,
    string Actor,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    bool IsActive);

public sealed record LocalServerPairingAbuseSummaryResponse(
    int TotalEventCount,
    int ActiveSourceDecisionCount,
    int RateLimitedEventCount,
    int RequestTooLargeEventCount,
    int DuplicateCoalescedEventCount,
    int PendingQueueFullEventCount,
    DateTimeOffset? LastEventAtUtc,
    IReadOnlyCollection<LocalServerPairingAbuseSourceDecisionResponse> ActiveSourceDecisions);

public sealed record ChangeLocalServerPairingAbuseSourceRequest(
    string? Actor = null,
    string? Reason = null,
    int? ExpiresInMinutes = null);

public sealed record LocalServerPairingAbuseSourceResponse(
    LocalServerPairingAbuseSourceDecisionResponse SourceDecision);

public sealed record CreateLocalServerManagerSessionRequest(
    Guid DeviceId,
    string DeviceCredential,
    string? RequestedBy = null);

public sealed record LocalServerManagerSessionResponse(
    string TokenType,
    string AccessToken,
    Guid SessionId,
    Guid ClientId,
    string InstallationId,
    Guid DeviceId,
    string Actor,
    string? AssignedRole,
    string SigningKeyId,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc);

public sealed record ApproveLocalServerDeviceRequest(
    string ApprovedBy,
    string AssignedRole = "ManagerApprovedDevice");

public sealed record ApproveLocalServerDeviceResponse(
    LocalServerDeviceResponse Device,
    string? DeviceCredential,
    LocalServerSignedDeviceCredentialResponse? SignedDeviceCredential = null);

public sealed record ChangeLocalServerDeviceStatusRequest(
    string Actor,
    string Reason);

public sealed record LocalServerDeviceResponse(
    Guid PairingRequestId,
    Guid DeviceId,
    Guid ClientId,
    string InstallationId,
    string PairingRequestStatus,
    string DeviceStatus,
    string DeviceDisplayName,
    string? DeviceFingerprintHash,
    string? WindowsUserHint,
    string? AppVersion,
    string? DevicePublicKeySha256,
    string? AssignedRole,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? ApprovedAtUtc,
    string? ApprovedBy,
    string? DeviceCredentialId,
    DateTimeOffset? DeviceCredentialIssuedAtUtc,
    DateTimeOffset? SuspendedAtUtc,
    string? SuspendedBy,
    string? SuspensionReason,
    DateTimeOffset? RevokedAtUtc,
    string? RevokedBy,
    string? RevocationReason);

public sealed record LocalServerPairingProfile(
    string FormatVersion,
    Guid ClientId,
    string InstallationId,
    string SiteId,
    Guid ApprovedDeviceId,
    string DevicePrivateKeyRef,
    string DeviceCredential,
    string? ServerPairingKeySha256,
    string? TlsCaSha256,
    string? TlsCertificateSha256,
    string? LastGoodUrl,
    IReadOnlyCollection<string> UrlCandidates,
    DateTimeOffset ApprovedAtUtc,
    DateTimeOffset LastSeenAtUtc);

public sealed record LocalServerDeviceCredentialPayloadResponse(
    string FormatVersion,
    Guid CredentialId,
    Guid ClientId,
    string InstallationId,
    Guid PairingRequestId,
    Guid DeviceId,
    string DevicePublicKeySha256,
    string AssignedRole,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset? ExpiresAtUtc);

public sealed record LocalServerSignedDeviceCredentialResponse(
    string PayloadJson,
    LocalServerDeviceCredentialPayloadResponse Payload,
    LocalServerBootstrapPackageSignatureResponse Signature,
    string CompactToken);

public sealed record VerifyLocalServerDeviceCredentialRequest(
    string? DeviceCredential = null);

public sealed record RefreshLocalServerDeviceCredentialRequest(
    string? DeviceCredential = null,
    string? RequestedBy = null);

public sealed record VerifyLocalServerDeviceCredentialResponse(
    Guid ClientId,
    string InstallationId,
    Guid PairingRequestId,
    Guid DeviceId,
    string DeviceStatus,
    string AssignedRole,
    string DeviceCredentialId,
    bool IsManagerCapable,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset VerifiedAtUtc);

public sealed record RefreshLocalServerDeviceCredentialResponse(
    Guid ClientId,
    string InstallationId,
    Guid PairingRequestId,
    Guid DeviceId,
    string DeviceStatus,
    string AssignedRole,
    bool IsManagerCapable,
    bool Rotated,
    string? DeviceCredential,
    LocalServerSignedDeviceCredentialResponse? SignedDeviceCredential,
    string CurrentDeviceCredentialId,
    DateTimeOffset CurrentCredentialIssuedAtUtc,
    DateTimeOffset? CurrentCredentialExpiresAtUtc,
    string? PreviousDeviceCredentialId,
    DateTimeOffset? PreviousCredentialGraceUntilUtc,
    DateTimeOffset RefreshedAtUtc);

public sealed record LocalServerFirstManagerSetupTokenPayloadResponse(
    string FormatVersion,
    Guid TokenId,
    Guid ClientId,
    string InstallationId,
    Guid PendingDeviceRequestId,
    IReadOnlyCollection<string> AllowedActions,
    string ManagerDisplayName,
    string? ManagerEmail,
    string CreatedBy,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string Purpose = LocalServerFirstManagerSetupTokenPurposes.FirstManagerBootstrap,
    string? RecoveryReason = null);

public sealed record IssueLocalServerFirstManagerSetupTokenRequest(
    Guid PendingDeviceRequestId,
    string ManagerDisplayName,
    string? ManagerEmail = null,
    string? CreatedBy = null,
    int ExpiresInHours = 24,
    string Purpose = LocalServerFirstManagerSetupTokenPurposes.FirstManagerBootstrap,
    string? RecoveryReason = null);

public sealed record LocalServerSignedFirstManagerSetupTokenResponse(
    string PayloadJson,
    LocalServerFirstManagerSetupTokenPayloadResponse Payload,
    LocalServerBootstrapPackageSignatureResponse Signature);

public sealed record IssueLocalServerFirstManagerSetupTokenResponse(
    Guid TokenId,
    Guid ClientId,
    string InstallationId,
    Guid PendingDeviceRequestId,
    string ManagerDisplayName,
    string? ManagerEmail,
    string CreatedBy,
    string SigningKeyId,
    string PayloadSha256,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    LocalServerSignedFirstManagerSetupTokenResponse SignedToken,
    string Purpose = LocalServerFirstManagerSetupTokenPurposes.FirstManagerBootstrap,
    string? RecoveryReason = null,
    IReadOnlyCollection<string>? AllowedActions = null);

public sealed record ImportLocalServerFirstManagerSetupTokenResponse(
    Guid TokenId,
    Guid ClientId,
    string InstallationId,
    Guid PairingRequestId,
    Guid DeviceId,
    string ManagerDisplayName,
    string? ManagerEmail,
    string CreatedBy,
    LocalServerDeviceResponse Device,
    string DeviceCredential,
    string SignatureKeyId,
    string PayloadSha256,
    DateTimeOffset ImportedAtUtc,
    LocalServerSignedDeviceCredentialResponse? SignedDeviceCredential = null,
    string Purpose = LocalServerFirstManagerSetupTokenPurposes.FirstManagerBootstrap,
    string? RecoveryReason = null,
    IReadOnlyCollection<string>? AllowedActions = null,
    string? AssignedRole = null);
