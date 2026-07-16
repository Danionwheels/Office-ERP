namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed class AcceptClientPortalInvitationResult
{
    private AcceptClientPortalInvitationResult(
        Guid userId,
        Guid clientId,
        string email,
        string fullName,
        string role,
        string? accessToken,
        string? refreshToken,
        DateTimeOffset? expiresAtUtc,
        DateTimeOffset? idleExpiresAtUtc,
        string? failureCode,
        string? detail)
    {
        UserId = userId;
        ClientId = clientId;
        Email = email;
        FullName = fullName;
        Role = role;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAtUtc = expiresAtUtc;
        IdleExpiresAtUtc = idleExpiresAtUtc;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => AccessToken is not null;

    public Guid UserId { get; }

    public Guid ClientId { get; }

    public string Email { get; }

    public string FullName { get; }

    public string Role { get; }

    public string? AccessToken { get; }

    public string? RefreshToken { get; }

    public DateTimeOffset? ExpiresAtUtc { get; }

    public DateTimeOffset? IdleExpiresAtUtc { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static AcceptClientPortalInvitationResult Success(
        Guid userId,
        Guid clientId,
        string email,
        string fullName,
        string role,
        string accessToken,
        string refreshToken,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset idleExpiresAtUtc)
    {
        return new AcceptClientPortalInvitationResult(
            userId,
            clientId,
            email,
            fullName,
            role,
            accessToken,
            refreshToken,
            expiresAtUtc,
            idleExpiresAtUtc,
            failureCode: null,
            detail: null);
    }

    public static AcceptClientPortalInvitationResult Failure(
        string failureCode,
        string detail)
    {
        return new AcceptClientPortalInvitationResult(
            Guid.Empty,
            Guid.Empty,
            email: "",
            fullName: "",
            role: "",
            accessToken: null,
            refreshToken: null,
            expiresAtUtc: null,
            idleExpiresAtUtc: null,
            failureCode,
            detail);
    }
}
