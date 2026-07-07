namespace SafarSuite.ControlCloud.Api.Modules.ProviderAccess;

public static class ProviderAccessEndpoints
{
    public static IEndpointRouteBuilder MapProviderAccessEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/provider-access")
            .WithTags("Provider Access");

        group.MapPost("/sessions", CreateSessionAsync)
            .WithName("CreateProviderAccessSession");
        group.MapPost("/operator-sessions", CreateOperatorSessionAsync)
            .WithName("CreateProviderOperatorSession");

        return endpoints;
    }

    private static Task<IResult> CreateSessionAsync(
        CreateProviderAccessSessionRequest request,
        ProviderAccessSessionService sessions)
    {
        var result = sessions.CreateSession(
            request.SharedSecret,
            request.Actor,
            request.Scopes,
            request.ExpiresInMinutes);

        if (!result.IsSuccess)
        {
            return Task.FromResult<IResult>(Results.Json(
                new { code = result.FailureCode, detail = result.Detail },
                statusCode: result.StatusCode));
        }

        return Task.FromResult<IResult>(Results.Ok(new CreateProviderAccessSessionResponse(
            result.AccessToken!,
            "Bearer",
            result.Actor!,
            result.Scopes!,
            result.ExpiresAtUtc!.Value)));
    }

    private static Task<IResult> CreateOperatorSessionAsync(
        CreateProviderOperatorSessionRequest request,
        ProviderAccessSessionService sessions)
    {
        var result = sessions.CreateSessionFromCredentials(
            request.Email,
            request.Password,
            request.Scopes,
            request.ExpiresInMinutes);

        if (!result.IsSuccess)
        {
            return Task.FromResult<IResult>(Results.Json(
                new { code = result.FailureCode, detail = result.Detail },
                statusCode: result.StatusCode));
        }

        return Task.FromResult<IResult>(Results.Ok(new CreateProviderAccessSessionResponse(
            result.AccessToken!,
            "Bearer",
            result.Actor!,
            result.Scopes!,
            result.ExpiresAtUtc!.Value)));
    }

    public sealed record CreateProviderAccessSessionRequest(
        string SharedSecret,
        string? Actor,
        string[]? Scopes = null,
        int? ExpiresInMinutes = null);

    public sealed record CreateProviderOperatorSessionRequest(
        string Email,
        string Password,
        string[]? Scopes = null,
        int? ExpiresInMinutes = null);

    public sealed record CreateProviderAccessSessionResponse(
        string AccessToken,
        string TokenType,
        string Actor,
        IReadOnlyCollection<string> Scopes,
        DateTimeOffset ExpiresAtUtc);
}
