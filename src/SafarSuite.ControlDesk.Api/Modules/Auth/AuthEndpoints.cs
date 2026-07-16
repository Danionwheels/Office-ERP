using System.Security.Cryptography;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Auth;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Common;

namespace SafarSuite.ControlDesk.Api.Modules.Auth;

public static class AuthEndpoints
{
    private const int MinimumSessionMinutes = 5;
    private const int MaximumSessionMinutes = 1_440;
    private const int SessionTokenBytes = 32;
    private const string LocalOperatorTokenType = "LocalOperator";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/auth")
            .WithTags("Auth");

        group.MapPost("/operator-sessions", CreateOperatorSession);

        return endpoints;
    }

    private static IResult CreateOperatorSession(
        CreateLocalOperatorSessionRequest request,
        IConfiguration configuration)
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

        var options = configuration
            .GetSection(ControlDeskOperatorAccessOptions.SectionName)
            .Get<ControlDeskOperatorAccessOptions>() ?? new ControlDeskOperatorAccessOptions();

        var sessionMinutes = request.ExpiresInMinutes ?? options.SessionMinutes;

        if (sessionMinutes is < MinimumSessionMinutes or > MaximumSessionMinutes)
        {
            return ValidationError(
                "expiresInMinutes",
                $"Session minutes must be between {MinimumSessionMinutes} and {MaximumSessionMinutes}.");
        }

        var operatorUser = options.Users.FirstOrDefault(user =>
            string.Equals(user.Email.Trim(), email, StringComparison.OrdinalIgnoreCase));

        if (operatorUser is null
            || !string.Equals(operatorUser.Status, "Active", StringComparison.OrdinalIgnoreCase)
            || !VerifyPassword(request.Password, operatorUser.PasswordHash))
        {
            return SignInError();
        }

        var scopes = operatorUser.Scopes
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(scope => scope.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .DefaultIfEmpty("control-desk:operator")
            .ToArray();

        var response = new LocalOperatorSessionResponse(
            AccessToken: CreateSessionToken(),
            TokenType: LocalOperatorTokenType,
            Actor: string.IsNullOrWhiteSpace(operatorUser.FullName)
                ? operatorUser.Email
                : operatorUser.FullName,
            Email: operatorUser.Email,
            Scopes: scopes,
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(sessionMinutes));

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

    private static string CreateSessionToken()
    {
        return Base64UrlEncode(RandomNumberGenerator.GetBytes(SessionTokenBytes));
    }

    private static string Base64UrlEncode(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
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

    private sealed class ControlDeskOperatorAccessOptions
    {
        public const string SectionName = "ControlDesk:OperatorAccess";

        public int SessionMinutes { get; set; } = 480;

        public List<ControlDeskOperatorUserOptions> Users { get; set; } = [];
    }

    private sealed class ControlDeskOperatorUserOptions
    {
        public string UserId { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Status { get; set; } = "Active";

        public List<string> Scopes { get; set; } = [];
    }
}
