using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class PersistentClientPortalSessionService : IClientPortalSessionService
{
    private const string TokenType = "client_portal_access";
    private const string Issuer = "SafarSuite.ControlCloud";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ClientPortalAccessOptions _options;
    private readonly IControlCloudClock _clock;
    private readonly IClientPortalCredentialService _credentials;
    private readonly IClientPortalSessionRepository _sessions;
    private readonly IClientPortalIdentityRepository _identities;
    private readonly IControlCloudUnitOfWork _unitOfWork;

    public PersistentClientPortalSessionService(
        ClientPortalAccessOptions options,
        IControlCloudClock clock,
        IClientPortalCredentialService credentials,
        IClientPortalSessionRepository sessions,
        IClientPortalIdentityRepository identities,
        IControlCloudUnitOfWork unitOfWork)
    {
        _options = options;
        _clock = clock;
        _credentials = credentials;
        _sessions = sessions;
        _identities = identities;
        _unitOfWork = unitOfWork;
    }

    public Task<CreateClientPortalSessionResult> CreateSessionAsync(
        Guid clientId,
        string role,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateClientPortalSessionResult.Failure(
            "PortalSessionSubjectRequired",
            "A stable Client Portal user is required before a session can be created."));
    }

    public async Task<CreateClientPortalSessionResult> CreateSessionAsync(
        Guid userId,
        Guid clientId,
        string role,
        int securityVersion,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            return NotConfigured();
        }

        var now = _clock.UtcNow;
        var refreshToken = _credentials.CreateSecureToken(_options.RefreshTokenBytes);
        var session = ControlCloudClientPortalSession.Create(
            Guid.NewGuid(),
            userId,
            clientId,
            role,
            securityVersion,
            HashRefreshToken(refreshToken),
            now,
            IdleTimeout,
            AbsoluteTimeout);
        await _sessions.AddAsync(session, cancellationToken);

        return Issue(session, refreshToken, now);
    }

    public ClientPortalSessionValidationResult Validate(string? authorizationHeader)
    {
        return ValidateAsync(authorizationHeader).GetAwaiter().GetResult();
    }

    public async Task<ClientPortalSessionValidationResult> ValidateAsync(
        string? authorizationHeader,
        bool touchActivity = true,
        CancellationToken cancellationToken = default)
    {
        var parsed = ParseAccessToken(authorizationHeader);

        if (!parsed.IsSuccess)
        {
            return parsed.Validation!;
        }

        var payload = parsed.Payload!;
        var now = _clock.UtcNow;

        if (payload.ExpiresAtUtc <= now)
        {
            return Failure("PortalSessionExpired", "Client Portal access token has expired.");
        }

        var session = await _sessions.GetByIdAsync(payload.SessionId, cancellationToken);
        var user = session is null
            ? null
            : await _identities.GetUserByIdAsync(session.UserId, cancellationToken);

        if (session is null || user is null)
        {
            return Failure("PortalSessionInvalid", "Client Portal session is no longer available.");
        }

        if (!string.Equals(user.Status, ControlCloudClientPortalUserStatuses.Active, StringComparison.Ordinal)
            || !session.IsActiveAt(now, user.SecurityVersion)
            || payload.UserId != session.UserId
            || payload.ClientId != session.ClientId
            || payload.SecurityVersion != session.SecurityVersion)
        {
            session.Revoke(now, "Session subject or timeout is no longer valid.");
            await _sessions.SaveAsync(session, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Failure("PortalSessionExpired", "Client Portal session has expired or was revoked.");
        }

        if (touchActivity)
        {
            session.Touch(now, IdleTimeout);
            await _sessions.SaveAsync(session, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ClientPortalSessionValidationResult.Success(
            new ClientPortalSessionPrincipal(
                session.ClientId,
                session.Role,
                payload.ExpiresAtUtc,
                session.UserId,
                session.SessionId,
                session.SecurityVersion,
                session.IdleExpiresAtUtc));
    }

    public async Task<CreateClientPortalSessionResult> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return CreateClientPortalSessionResult.Failure(
                "PortalRefreshRequired",
                "A refresh token is required.");
        }

        var hash = HashRefreshToken(refreshToken);
        var session = await _sessions.GetByRefreshTokenHashAsync(hash, cancellationToken);

        if (session is null)
        {
            return CreateClientPortalSessionResult.Failure(
                "PortalRefreshInvalid",
                "The refresh token is invalid or has already been rotated.");
        }

        var now = _clock.UtcNow;

        if (string.Equals(session.PreviousRefreshTokenHash, hash, StringComparison.Ordinal))
        {
            session.Revoke(now, "Rotated refresh token reuse detected.");
            await _sessions.SaveAsync(session, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return CreateClientPortalSessionResult.Failure(
                "PortalRefreshReused",
                "The refresh token was already used; the session has been revoked.");
        }

        var user = await _identities.GetUserByIdAsync(session.UserId, cancellationToken);

        if (user is null
            || !string.Equals(user.Status, ControlCloudClientPortalUserStatuses.Active, StringComparison.Ordinal)
            || !session.IsActiveAt(now, user.SecurityVersion))
        {
            session.Revoke(now, "Session subject or timeout is no longer valid.");
            await _sessions.SaveAsync(session, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return CreateClientPortalSessionResult.Failure(
                "PortalRefreshExpired",
                "The session has expired or was revoked.");
        }

        var replacement = _credentials.CreateSecureToken(_options.RefreshTokenBytes);
        session.RotateRefreshToken(HashRefreshToken(replacement), now, IdleTimeout);
        await _sessions.SaveAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Issue(session, replacement, now);
    }

    public async Task<bool> RevokeCurrentAsync(
        string? authorizationHeader,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(
            authorizationHeader,
            touchActivity: false,
            cancellationToken);

        if (!validation.IsSuccess)
        {
            return false;
        }

        var session = await _sessions.GetByIdAsync(
            validation.Principal!.SessionId,
            cancellationToken);

        if (session is null)
        {
            return false;
        }

        session.Revoke(_clock.UtcNow, reason);
        await _sessions.SaveAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> RevokeAllForUserAsync(
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var sessions = await _sessions.ListByUserIdAsync(userId, token);
                var now = _clock.UtcNow;
                var count = 0;

                foreach (var session in sessions.Where(session => session.RevokedAtUtc is null))
                {
                    session.Revoke(now, reason);
                    await _sessions.SaveAsync(session, token);
                    count++;
                }

                return count;
            },
            cancellationToken);
    }

    private CreateClientPortalSessionResult Issue(
        ControlCloudClientPortalSession session,
        string refreshToken,
        DateTimeOffset now)
    {
        var expiresAt = Min(now.Add(AccessTokenTimeout), session.IdleExpiresAtUtc, session.AbsoluteExpiresAtUtc);
        var payload = new AccessTokenPayload(
            TokenType,
            Issuer,
            session.SessionId,
            session.UserId,
            session.ClientId,
            session.Role,
            session.SecurityVersion,
            now,
            expiresAt);
        var payloadText = Encode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOptions)));
        var accessToken = $"{payloadText}.{Sign(payloadText)}";

        return CreateClientPortalSessionResult.Success(
            session.UserId,
            session.ClientId,
            accessToken,
            refreshToken,
            expiresAt,
            session.IdleExpiresAtUtc,
            session.Role);
    }

    private ParsedToken ParseAccessToken(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader)
            || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return ParsedToken.Fail(Failure("PortalSessionRequired", "Client Portal session is required."));
        }

        var parts = authorizationHeader["Bearer ".Length..].Trim().Split('.', 2);

        if (parts.Length != 2 || !FixedTimeEquals(parts[1], Sign(parts[0])))
        {
            return ParsedToken.Fail(Failure("PortalSessionInvalid", "Client Portal access token is invalid."));
        }

        try
        {
            var payload = JsonSerializer.Deserialize<AccessTokenPayload>(Decode(parts[0]), JsonOptions);

            if (payload is null
                || payload.Type != TokenType
                || payload.Issuer != Issuer
                || payload.SessionId == Guid.Empty
                || payload.UserId == Guid.Empty
                || payload.ClientId == Guid.Empty)
            {
                return ParsedToken.Fail(Failure("PortalSessionInvalid", "Client Portal access token payload is invalid."));
            }

            return ParsedToken.Ok(payload);
        }
        catch (FormatException)
        {
            return ParsedToken.Fail(Failure("PortalSessionInvalid", "Client Portal access token payload is invalid."));
        }
        catch (JsonException)
        {
            return ParsedToken.Fail(Failure("PortalSessionInvalid", "Client Portal access token payload is invalid."));
        }
    }

    private string HashRefreshToken(string token) =>
        _credentials.HashSecret($"client-portal-refresh:{token.Trim()}");

    private bool IsConfigured() => !string.IsNullOrWhiteSpace(_options.SessionSigningSecret);

    private CreateClientPortalSessionResult NotConfigured() =>
        CreateClientPortalSessionResult.Failure(
            "PortalSessionNotConfigured",
            "Client Portal session signing is not configured.");

    private string Sign(string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SessionSigningSecret));
        return Encode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string Encode(byte[] value) => Convert.ToBase64String(value)
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Decode(string value)
    {
        var text = value.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(text.PadRight(text.Length + ((4 - text.Length % 4) % 4), '='));
    }

    private static ClientPortalSessionValidationResult Failure(string code, string detail) =>
        ClientPortalSessionValidationResult.Failure(code, detail);

    private static DateTimeOffset Min(params DateTimeOffset[] values) => values.Min();

    private TimeSpan AccessTokenTimeout => TimeSpan.FromMinutes(Math.Clamp(_options.AccessTokenMinutes, 1, 60));
    private TimeSpan IdleTimeout => TimeSpan.FromMinutes(Math.Clamp(_options.SessionIdleTimeoutMinutes, 5, 1440));
    private TimeSpan AbsoluteTimeout => TimeSpan.FromMinutes(Math.Clamp(_options.SessionAbsoluteTimeoutMinutes, 30, 10080));

    private sealed record AccessTokenPayload(
        string Type,
        string Issuer,
        Guid SessionId,
        Guid UserId,
        Guid ClientId,
        string Role,
        int SecurityVersion,
        DateTimeOffset IssuedAtUtc,
        DateTimeOffset ExpiresAtUtc);

    private sealed record ParsedToken(
        AccessTokenPayload? Payload,
        ClientPortalSessionValidationResult? Validation)
    {
        public bool IsSuccess => Payload is not null;
        public static ParsedToken Ok(AccessTokenPayload payload) => new(payload, null);
        public static ParsedToken Fail(ClientPortalSessionValidationResult validation) => new(null, validation);
    }
}
