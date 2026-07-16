namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

public interface IClientPortalSessionService
{
    Task<CreateClientPortalSessionResult> CreateSessionAsync(
        Guid clientId,
        string role,
        CancellationToken cancellationToken = default);

    Task<CreateClientPortalSessionResult> CreateSessionAsync(
        Guid userId,
        Guid clientId,
        string role,
        int securityVersion,
        CancellationToken cancellationToken = default)
    {
        return CreateSessionAsync(clientId, role, cancellationToken);
    }

    ClientPortalSessionValidationResult Validate(string? authorizationHeader);

    Task<ClientPortalSessionValidationResult> ValidateAsync(
        string? authorizationHeader,
        bool touchActivity = true,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Validate(authorizationHeader));
    }

    Task<CreateClientPortalSessionResult> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateClientPortalSessionResult.Failure(
            "PortalRefreshNotSupported",
            "Client Portal session refresh is not supported."));
    }

    Task<bool> RevokeCurrentAsync(
        string? authorizationHeader,
        string reason,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    Task<int> RevokeAllForUserAsync(
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }
}

public sealed record ClientPortalSessionPrincipal(
    Guid ClientId,
    string Role,
    DateTimeOffset ExpiresAtUtc,
    Guid UserId = default,
    Guid SessionId = default,
    int SecurityVersion = 1,
    DateTimeOffset? IdleExpiresAtUtc = null);

public sealed class ClientPortalSessionValidationResult
{
    private ClientPortalSessionValidationResult(
        ClientPortalSessionPrincipal? principal,
        string? failureCode,
        string? detail)
    {
        Principal = principal;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Principal is not null;

    public ClientPortalSessionPrincipal? Principal { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ClientPortalSessionValidationResult Success(
        ClientPortalSessionPrincipal principal)
    {
        return new ClientPortalSessionValidationResult(
            principal,
            failureCode: null,
            detail: null);
    }

    public static ClientPortalSessionValidationResult Failure(
        string failureCode,
        string detail)
    {
        return new ClientPortalSessionValidationResult(
            principal: null,
            failureCode,
            detail);
    }
}
