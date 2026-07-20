using Microsoft.Extensions.Options;
using SafarSuite.ControlDesk.Application.Modules.Auth.AuthenticateLocalOperator;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Auth;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Common;

namespace SafarSuite.ControlDesk.Api.Modules.Auth;

public static class AuthEndpoints
{
    private const int MinimumSessionMinutes = 5;
    private const int MaximumSessionMinutes = 1_440;
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/auth")
            .WithTags("Auth");

        group.MapPost("/operator-sessions", CreateOperatorSession)
            .AllowAnonymous();

        return endpoints;
    }

    private static async Task<IResult> CreateOperatorSession(
        CreateLocalOperatorSessionRequest request,
        IOptions<ControlDeskOperatorAccessOptions> optionsAccessor,
        AuthenticateLocalOperatorHandler authentication,
        IControlDeskSessionTokenService tokens,
        CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim() ?? string.Empty;

        if (email.Length == 0)
        {
            return ValidationError("email", "Email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return ValidationError("password", "Password is required.");
        }

        var options = optionsAccessor.Value;

        var sessionMinutes = request.ExpiresInMinutes ?? options.SessionMinutes;

        if (sessionMinutes is < MinimumSessionMinutes or > MaximumSessionMinutes)
        {
            return ValidationError(
                "expiresInMinutes",
                $"Session minutes must be between {MinimumSessionMinutes} and {MaximumSessionMinutes}.");
        }

        var authenticationResult = await authentication.HandleAsync(
            new AuthenticateLocalOperatorCommand(email, request.Password),
            cancellationToken);

        if (!authenticationResult.IsAuthenticated || authenticationResult.Principal is null)
        {
            return SignInError();
        }

        var session = tokens.Issue(authenticationResult.Principal, sessionMinutes);

        var response = new LocalOperatorSessionResponse(
            AccessToken: session.AccessToken,
            TokenType: "Bearer",
            Actor: session.Actor,
            Email: session.Email,
            Roles: session.Roles,
            Scopes: session.Scopes,
            ExpiresAtUtc: session.ExpiresAtUtc);

        return Results.Ok(response);
    }

    private static IResult ValidationError(string target, string message)
    {
        return Results.Json(
            new ApiErrorResponse(
                StatusCodes.Status400BadRequest,
                "Request validation failed.",
                new[] { new ApiErrorItem("validation", message, target) }),
            statusCode: StatusCodes.Status400BadRequest);
    }

    private static IResult SignInError()
    {
        return Results.Json(
            new ApiErrorResponse(
                StatusCodes.Status400BadRequest,
                "Request validation failed.",
                new[] { new ApiErrorItem("validation", "Local operator email or password is invalid.", "password") }),
            statusCode: StatusCodes.Status400BadRequest);
    }

}
