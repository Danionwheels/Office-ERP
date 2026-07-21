namespace SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

public sealed record CreateClientPortalInvitationRequest(
    Guid ClientId,
    string Email,
    string FullName,
    string Role,
    int ExpiresInDays,
    string CreatedBy);

public sealed record ClientPortalInvitationResponse(
    Guid InvitationId,
    Guid ClientId,
    string Email,
    string FullName,
    string Role,
    string Status,
    DateTimeOffset InvitedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string? InvitationToken,
    string? InvitationUrl);

public sealed record ListClientPortalInvitationsResponse(
    Guid ClientId,
    IReadOnlyCollection<ClientPortalInvitationResponse> Invitations);

public sealed record ResendClientPortalInvitationRequest(
    int ExpiresInDays,
    string CreatedBy);

public sealed record RevokeClientPortalInvitationRequest(
    string RevokedBy);

public sealed record AcceptClientPortalInvitationRequest(
    string InvitationToken,
    string Password,
    string? FullName);

public sealed record AcceptClientPortalInvitationResponse(
    Guid UserId,
    Guid ClientId,
    string Email,
    string FullName,
    string Role,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset IdleExpiresAtUtc);

public sealed record CreateClientPortalSessionRequest(
    Guid ClientId,
    string Email,
    string Password,
    string? TotpCode = null,
    string? RecoveryCode = null);

public sealed record ClientPortalSessionResponse(
    Guid UserId,
    Guid ClientId,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset IdleExpiresAtUtc,
    string Role);

public sealed record RefreshClientPortalSessionRequest(string RefreshToken);

public sealed record BeginClientPortalMfaEnrollmentResponse(
    string Secret,
    string OtpAuthUri,
    string QrCodeDataUri,
    IReadOnlyCollection<string> RecoveryCodes);

public sealed record BeginClientPortalMfaEnrollmentRequest(string Password);

public sealed record ConfirmClientPortalMfaEnrollmentRequest(string Code);

public sealed record RequestClientPortalPasswordResetRequest(Guid ClientId, string Email);

public sealed record ValidateClientPortalPasswordResetRequest(string ResetToken);

public sealed record CompleteClientPortalPasswordResetRequest(string ResetToken, string NewPassword);

public sealed record ClientPortalPasswordResetValidationResponse(bool IsValid);
