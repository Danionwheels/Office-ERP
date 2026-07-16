namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed class CreateClientPortalSessionResult
{
    private CreateClientPortalSessionResult(
        Guid clientId,
        Guid userId,
        string? accessToken,
        string? refreshToken,
        DateTimeOffset? expiresAtUtc,
        DateTimeOffset? idleExpiresAtUtc,
        string? role,
        string? failureCode,
        string? detail)
    {
        ClientId = clientId;
        UserId = userId;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAtUtc = expiresAtUtc;
        IdleExpiresAtUtc = idleExpiresAtUtc;
        Role = role;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => AccessToken is not null;

    public Guid ClientId { get; }

    public Guid UserId { get; }

    public string? AccessToken { get; }

    public string? RefreshToken { get; }

    public DateTimeOffset? ExpiresAtUtc { get; }

    public DateTimeOffset? IdleExpiresAtUtc { get; }

    public string? Role { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static CreateClientPortalSessionResult Success(
        Guid clientId,
        string accessToken,
        DateTimeOffset expiresAtUtc,
        string role)
    {
        return new CreateClientPortalSessionResult(
            clientId,
            Guid.Empty,
            accessToken,
            refreshToken: null,
            expiresAtUtc,
            idleExpiresAtUtc: expiresAtUtc,
            role,
            failureCode: null,
            detail: null);
    }

    public static CreateClientPortalSessionResult Success(
        Guid userId,
        Guid clientId,
        string accessToken,
        string refreshToken,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset idleExpiresAtUtc,
        string role)
    {
        return new CreateClientPortalSessionResult(
            clientId,
            userId,
            accessToken,
            refreshToken,
            expiresAtUtc,
            idleExpiresAtUtc,
            role,
            failureCode: null,
            detail: null);
    }

    public static CreateClientPortalSessionResult Failure(
        string failureCode,
        string detail)
    {
        return new CreateClientPortalSessionResult(
            Guid.Empty,
            Guid.Empty,
            accessToken: null,
            refreshToken: null,
            expiresAtUtc: null,
            idleExpiresAtUtc: null,
            role: null,
            failureCode,
            detail);
    }
}
