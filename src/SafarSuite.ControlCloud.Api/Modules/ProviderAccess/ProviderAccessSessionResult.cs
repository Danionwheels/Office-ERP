namespace SafarSuite.ControlCloud.Api.Modules.ProviderAccess;

public sealed class ProviderAccessSessionResult
{
    private ProviderAccessSessionResult(
        string? accessToken,
        string? actor,
        IReadOnlyCollection<string>? scopes,
        DateTimeOffset? expiresAtUtc,
        string? failureCode,
        string? detail,
        int statusCode)
    {
        AccessToken = accessToken;
        Actor = actor;
        Scopes = scopes;
        ExpiresAtUtc = expiresAtUtc;
        FailureCode = failureCode;
        Detail = detail;
        StatusCode = statusCode;
    }

    public bool IsSuccess => !string.IsNullOrWhiteSpace(AccessToken);

    public string? AccessToken { get; }

    public string? Actor { get; }

    public IReadOnlyCollection<string>? Scopes { get; }

    public DateTimeOffset? ExpiresAtUtc { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public int StatusCode { get; }

    public static ProviderAccessSessionResult Success(
        string accessToken,
        string actor,
        IReadOnlyCollection<string> scopes,
        DateTimeOffset expiresAtUtc)
    {
        return new ProviderAccessSessionResult(
            accessToken,
            actor,
            scopes,
            expiresAtUtc,
            failureCode: null,
            detail: null,
            StatusCodes.Status200OK);
    }

    public static ProviderAccessSessionResult Failure(
        string failureCode,
        string detail,
        int statusCode)
    {
        return new ProviderAccessSessionResult(
            accessToken: null,
            actor: null,
            scopes: null,
            expiresAtUtc: null,
            failureCode,
            detail,
            statusCode);
    }
}
