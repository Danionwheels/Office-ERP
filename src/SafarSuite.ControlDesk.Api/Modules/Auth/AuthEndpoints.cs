using System.Security.Cryptography;
using Microsoft.Extensions.Options;
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

    private static IResult CreateOperatorSession(
        CreateLocalOperatorSessionRequest request,
        IOptions<ControlDeskOperatorAccessOptions> optionsAccessor,
        IControlDeskSessionTokenService tokens)
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

        var operatorUser = options.Users.FirstOrDefault(user =>
            string.Equals(user.Email?.Trim(), email, StringComparison.OrdinalIgnoreCase));

        if (operatorUser is null
            || !string.Equals(operatorUser.Status, "Active", StringComparison.OrdinalIgnoreCase)
            || !VerifyPassword(request.Password, operatorUser.PasswordHash))
        {
            return SignInError();
        }

        var session = tokens.Issue(operatorUser, sessionMinutes);

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

    private static bool VerifyPassword(string password, string passwordHash)
    {
        var parts = passwordHash.Split('.', 4);

        if (parts.Length != 4 || parts[0] != "pbkdf2-sha256")
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations) || iterations <= 0)
        {
            return false;
        }

        try
        {
            var salt = Base64UrlDecode(parts[2]);
            var expectedHash = Base64UrlDecode(parts[3]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var incoming = value
            .Replace('-', '+')
            .Replace('_', '/');
        var padding = incoming.Length % 4;

        if (padding > 0)
        {
            incoming = incoming.PadRight(incoming.Length + 4 - padding, '=');
        }

        return Convert.FromBase64String(incoming);
    }

}
