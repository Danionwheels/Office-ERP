namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

public interface IClientPortalSessionService
{
    Task<CreateClientPortalSessionResult> CreateSessionAsync(
        Guid clientId,
        string role,
        CancellationToken cancellationToken = default);

    ClientPortalSessionValidationResult Validate(string? authorizationHeader);
}

public sealed record ClientPortalSessionPrincipal(
    Guid ClientId,
    string Role,
    DateTimeOffset ExpiresAtUtc);

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
