namespace SafarSuite.LocalServer.Domain.Pairing;

public sealed record LocalServerPairingSecurityEvent(
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

public sealed record LocalServerPairingAbuseSourceDecision(
    string SourceKey,
    string Action,
    string Reason,
    string Actor,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc);
