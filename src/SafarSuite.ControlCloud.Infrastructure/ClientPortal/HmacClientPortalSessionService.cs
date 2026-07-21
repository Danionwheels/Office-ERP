using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class HmacClientPortalSessionService : IClientPortalSessionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ClientPortalAccessOptions _options;
    private readonly IControlCloudClock _clock;

    public HmacClientPortalSessionService(
        ClientPortalAccessOptions options,
        IControlCloudClock clock)
    {
        _options = options;
        _clock = clock;
    }

    public Task<CreateClientPortalSessionResult> CreateSessionAsync(
        Guid clientId,
        string role,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SessionSigningSecret))
        {
            return Task.FromResult(CreateClientPortalSessionResult.Failure(
                "PortalSessionNotConfigured",
                "Client Portal session signing is not configured."));
        }

        var now = _clock.UtcNow;
        var expiresAtUtc = now.AddMinutes(Math.Max(5, _options.AccessTokenMinutes));
        var payload = new ClientPortalSessionTokenPayload(
            clientId,
            NormalizeRole(role),
            now,
            expiresAtUtc);
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
        var payloadText = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signature = Sign(payloadText);
        var token = $"{payloadText}.{signature}";

        return Task.FromResult(CreateClientPortalSessionResult.Success(
            clientId,
            token,
            expiresAtUtc,
            payload.Role));
    }

    public ClientPortalSessionValidationResult Validate(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader)
            || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return ClientPortalSessionValidationResult.Failure(
                "PortalSessionRequired",
                "Client Portal session is required.");
        }

        var token = authorizationHeader["Bearer ".Length..].Trim();
        var parts = token.Split('.', 2);

        if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
        {
            return ClientPortalSessionValidationResult.Failure(
                "PortalSessionInvalid",
                "Client Portal session token is invalid.");
        }

        var expectedSignature = Sign(parts[0]);

        if (!FixedTimeEquals(parts[1], expectedSignature))
        {
            return ClientPortalSessionValidationResult.Failure(
                "PortalSessionInvalid",
                "Client Portal session signature is invalid.");
        }

        ClientPortalSessionTokenPayload? payload;

        try
        {
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[0]));
            payload = JsonSerializer.Deserialize<ClientPortalSessionTokenPayload>(
                payloadJson,
                JsonOptions);
        }
        catch (FormatException)
        {
            return ClientPortalSessionValidationResult.Failure(
                "PortalSessionInvalid",
                "Client Portal session payload is invalid.");
        }
        catch (JsonException)
        {
            return ClientPortalSessionValidationResult.Failure(
                "PortalSessionInvalid",
                "Client Portal session payload is invalid.");
        }

        if (payload is null || payload.ClientId == Guid.Empty)
        {
            return ClientPortalSessionValidationResult.Failure(
                "PortalSessionInvalid",
                "Client Portal session payload is invalid.");
        }

        if (payload.ExpiresAtUtc <= _clock.UtcNow)
        {
            return ClientPortalSessionValidationResult.Failure(
                "PortalSessionExpired",
                "Client Portal session has expired.");
        }

        return ClientPortalSessionValidationResult.Success(
            new ClientPortalSessionPrincipal(
                payload.ClientId,
                payload.Role,
                payload.ExpiresAtUtc));
    }

    private string Sign(string payloadText)
    {
        using var hmac = new HMACSHA256(
            Encoding.UTF8.GetBytes(_options.SessionSigningSecret));
        var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadText));

        return Base64UrlEncode(signature);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string Base64UrlEncode(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string NormalizeRole(string role)
    {
        var normalized = role.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? "ClientViewer" : normalized;
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

    private sealed record ClientPortalSessionTokenPayload(
        Guid ClientId,
        string Role,
        DateTimeOffset IssuedAtUtc,
        DateTimeOffset ExpiresAtUtc);
}
