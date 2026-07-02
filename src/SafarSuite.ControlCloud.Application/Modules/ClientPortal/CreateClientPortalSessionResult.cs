namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed class CreateClientPortalSessionResult
{
    private CreateClientPortalSessionResult(
        Guid clientId,
        string? accessToken,
        DateTimeOffset? expiresAtUtc,
        string? role,
        string? failureCode,
        string? detail)
    {
        ClientId = clientId;
        AccessToken = accessToken;
        ExpiresAtUtc = expiresAtUtc;
        Role = role;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => AccessToken is not null;

    public Guid ClientId { get; }

    public string? AccessToken { get; }

    public DateTimeOffset? ExpiresAtUtc { get; }

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
            accessToken,
            expiresAtUtc,
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
            accessToken: null,
            expiresAtUtc: null,
            role: null,
            failureCode,
            detail);
    }
}
