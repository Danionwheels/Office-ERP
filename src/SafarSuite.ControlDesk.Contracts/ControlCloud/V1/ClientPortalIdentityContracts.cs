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
    DateTimeOffset ExpiresAtUtc);

public sealed record CreateClientPortalSessionRequest(
    Guid ClientId,
    string Email,
    string Password);

public sealed record ClientPortalSessionResponse(
    Guid ClientId,
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    string Role);
