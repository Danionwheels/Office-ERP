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
        DateTimeOffset? expiresAtUtc,
        string? failureCode,
        string? detail)
    {
        UserId = userId;
        ClientId = clientId;
        Email = email;
        FullName = fullName;
        Role = role;
        AccessToken = accessToken;
        ExpiresAtUtc = expiresAtUtc;
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

    public DateTimeOffset? ExpiresAtUtc { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static AcceptClientPortalInvitationResult Success(
        Guid userId,
        Guid clientId,
        string email,
        string fullName,
        string role,
        string accessToken,
        DateTimeOffset expiresAtUtc)
    {
        return new AcceptClientPortalInvitationResult(
            userId,
            clientId,
            email,
            fullName,
            role,
            accessToken,
            expiresAtUtc,
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
            expiresAtUtc: null,
            failureCode,
            detail);
    }
}
