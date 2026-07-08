namespace SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

public static class LocalServerPairingFormats
{
    public const string DiscoveryVersion = "safarsuite-local-discovery-v1";
    public const string HelloRequestVersion = "safarsuite-local-pairing-hello-request-v1";
    public const string HelloResponseVersion = "safarsuite-local-pairing-hello-v1";
    public const string DevicePairingRequestVersion = "safarsuite-local-device-pairing-request-v1";
    public const string PairingProfileVersion = "safarsuite-local-pairing-profile-v1";
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
    DateTimeOffset? ExpiresAtUtc);

public sealed record LocalServerDevicePairingRequestsResponse(
    IReadOnlyCollection<LocalServerDeviceResponse> Devices);

public sealed record LocalServerDeviceRegisterResponse(
    IReadOnlyCollection<LocalServerDeviceResponse> Devices);

public sealed record ApproveLocalServerDeviceRequest(
    string ApprovedBy,
    string AssignedRole = "ManagerApprovedDevice");

public sealed record ApproveLocalServerDeviceResponse(
    LocalServerDeviceResponse Device,
    string? DeviceCredential);

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
