namespace SafarSuite.ControlCloud.Api.Modules.ProviderAccess;

public sealed class ProviderAccessAuthorizationResult
{
    private ProviderAccessAuthorizationResult(
        ProviderAccessPrincipal? principal,
        string? failureCode,
        string? detail,
        int statusCode)
    {
        Principal = principal;
        FailureCode = failureCode;
        Detail = detail;
        StatusCode = statusCode;
    }

    public bool IsSuccess => Principal is not null;

    public ProviderAccessPrincipal? Principal { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public int StatusCode { get; }

    public static ProviderAccessAuthorizationResult Success(ProviderAccessPrincipal principal)
    {
        return new ProviderAccessAuthorizationResult(
            principal,
            failureCode: null,
            detail: null,
            StatusCodes.Status200OK);
    }

    public static ProviderAccessAuthorizationResult Failure(
        string failureCode,
        string detail,
        int statusCode)
    {
        return new ProviderAccessAuthorizationResult(
            principal: null,
            failureCode,
            detail,
            statusCode);
    }
}
