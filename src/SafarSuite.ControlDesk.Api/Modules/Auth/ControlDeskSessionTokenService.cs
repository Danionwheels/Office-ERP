using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SafarSuite.ControlDesk.Application.Modules.Auth.AuthenticateLocalOperator;

namespace SafarSuite.ControlDesk.Api.Modules.Auth;

public interface IControlDeskSessionTokenService
{
    ControlDeskIssuedSession Issue(
        LocalOperatorSessionPrincipal localOperator,
        int sessionMinutes);

    ControlDeskSessionValidationResult Validate(string token);
}

public sealed record ControlDeskIssuedSession(
    string AccessToken,
    string Actor,
    string Email,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Scopes,
    DateTimeOffset ExpiresAtUtc);

public sealed record ControlDeskSessionValidationResult(
    bool IsValid,
    ControlDeskSessionTokenSnapshot? Snapshot,
    string? FailureCode)
{
    public static ControlDeskSessionValidationResult Success(
        ControlDeskSessionTokenSnapshot snapshot) =>
        new(true, snapshot, null);

    public static ControlDeskSessionValidationResult Failure(string code) =>
        new(false, null, code);
}

public sealed record ControlDeskSessionTokenSnapshot(
    Guid OperatorId,
    string Email,
    string Actor,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Scopes,
    long SecurityVersion);

public sealed class ControlDeskSessionTokenService(
    IOptions<ControlDeskOperatorAccessOptions> options,
    TimeProvider timeProvider) : IControlDeskSessionTokenService
{
    public const string AuthenticationScheme = "ControlDeskBearer";
    public const string ScopeClaimType = "scope";

    private const int TokenVersion = 2;
    private const int MaximumTokenLength = 16_384;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ControlDeskOperatorAccessOptions _options = options.Value;

    public ControlDeskIssuedSession Issue(
        LocalOperatorSessionPrincipal localOperator,
        int sessionMinutes)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(sessionMinutes);
        var actor = string.IsNullOrWhiteSpace(localOperator.FullName)
            ? localOperator.Email.Trim()
            : localOperator.FullName.Trim();
        var roles = Normalize(localOperator.Roles);
        var scopes = Normalize(localOperator.Scopes);
        var payload = new SessionTokenPayload(
            TokenVersion,
            localOperator.OperatorId,
            localOperator.Email.Trim(),
            actor,
            roles,
            scopes,
            localOperator.SecurityVersion,
            now.ToUnixTimeSeconds(),
            expiresAt.ToUnixTimeSeconds(),
            Base64UrlEncode(RandomNumberGenerator.GetBytes(16)));
        var encodedPayload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions));
        var signature = Sign(encodedPayload);

        return new ControlDeskIssuedSession(
            $"{encodedPayload}.{signature}",
            actor,
            payload.Email,
            roles,
            scopes,
            expiresAt);
    }

    public ControlDeskSessionValidationResult Validate(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > MaximumTokenLength)
        {
            return ControlDeskSessionValidationResult.Failure("SessionTokenInvalid");
        }

        var separatorIndex = token.IndexOf('.');

        if (separatorIndex <= 0 || separatorIndex != token.LastIndexOf('.'))
        {
            return ControlDeskSessionValidationResult.Failure("SessionTokenInvalid");
        }

        var encodedPayload = token[..separatorIndex];
        var suppliedSignature = token[(separatorIndex + 1)..];

        if (!FixedTimeEquals(Sign(encodedPayload), suppliedSignature))
        {
            return ControlDeskSessionValidationResult.Failure("SessionTokenSignatureInvalid");
        }

        SessionTokenPayload? payload;

        try
        {
            payload = JsonSerializer.Deserialize<SessionTokenPayload>(Base64UrlDecode(encodedPayload), JsonOptions);
        }
        catch (JsonException)
        {
            return ControlDeskSessionValidationResult.Failure("SessionTokenInvalid");
        }
        catch (FormatException)
        {
            return ControlDeskSessionValidationResult.Failure("SessionTokenInvalid");
        }

        if (payload is null
            || payload.Version != TokenVersion
            || payload.OperatorId == Guid.Empty
            || string.IsNullOrWhiteSpace(payload.Email)
            || string.IsNullOrWhiteSpace(payload.Actor)
            || payload.SecurityVersion <= 0
            || payload.ExpiresAtUnixSeconds <= payload.IssuedAtUnixSeconds)
        {
            return ControlDeskSessionValidationResult.Failure("SessionTokenInvalid");
        }

        var nowUnixSeconds = timeProvider.GetUtcNow().ToUnixTimeSeconds();

        if (payload.ExpiresAtUnixSeconds <= nowUnixSeconds)
        {
            return ControlDeskSessionValidationResult.Failure("SessionTokenExpired");
        }

        return ControlDeskSessionValidationResult.Success(new ControlDeskSessionTokenSnapshot(
            payload.OperatorId,
            payload.Email,
            payload.Actor,
            Normalize(payload.Roles),
            Normalize(payload.Scopes),
            payload.SecurityVersion));
    }

    private string Sign(string encodedPayload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SessionSigningSecret.Trim()));
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(encodedPayload)));
    }

    private static string[] Normalize(IEnumerable<string>? values) =>
        (values ?? [])
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private static bool FixedTimeEquals(string expected, string supplied)
    {
        try
        {
            var suppliedBytes = Base64UrlDecode(supplied);

            if (!string.Equals(Base64UrlEncode(suppliedBytes), supplied, StringComparison.Ordinal))
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(
                Base64UrlDecode(expected),
                suppliedBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var incoming = value.Replace('-', '+').Replace('_', '/');
        var padding = incoming.Length % 4;

        if (padding > 0)
        {
            incoming = incoming.PadRight(incoming.Length + 4 - padding, '=');
        }

        return Convert.FromBase64String(incoming);
    }

    private sealed record SessionTokenPayload(
        int Version,
        Guid OperatorId,
        string Email,
        string Actor,
        IReadOnlyCollection<string> Roles,
        IReadOnlyCollection<string> Scopes,
        long SecurityVersion,
        long IssuedAtUnixSeconds,
        long ExpiresAtUnixSeconds,
        string Nonce);
}
