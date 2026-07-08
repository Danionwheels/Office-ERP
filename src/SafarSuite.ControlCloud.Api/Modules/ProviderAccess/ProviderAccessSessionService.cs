using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SafarSuite.ControlCloud.Api.Modules.ClientPortal;
using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Api.Modules.ProviderAccess;

public sealed class ProviderAccessSessionService
{
    public const string ProviderAccessHeaderName = "X-SafarSuite-Provider-Key";
    private const string BearerPrefix = "Bearer ";
    private const string TokenType = "provider_access";
    private const string Issuer = "SafarSuite.ControlCloud";
    private const int MinLockoutThreshold = 2;
    private const int MaxLockoutThreshold = 20;
    private const int MinLockoutMinutes = 1;
    private const int MaxLockoutMinutes = 1440;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ClientPortalProviderAccessOptions _options;
    private readonly IControlCloudClock _clock;
    private readonly IClientPortalCredentialService _credentials;
    private readonly IProviderAccessOperatorStore _operators;
    private readonly IProviderAccessTotpSecretProtector _totpSecrets;

    public ProviderAccessSessionService(
        ClientPortalProviderAccessOptions options,
        IControlCloudClock clock,
        IClientPortalCredentialService credentials,
        IProviderAccessOperatorStore operators,
        IProviderAccessTotpSecretProtector totpSecrets)
    {
        _options = options;
        _clock = clock;
        _credentials = credentials;
        _operators = operators;
        _totpSecrets = totpSecrets;
    }

    public ProviderAccessSessionResult CreateSession(
        string? sharedSecret,
        string? actor,
        IEnumerable<string>? scopes,
        int? expiresInMinutes)
    {
        if (string.IsNullOrWhiteSpace(_options.SharedSecret))
        {
            return ProviderAccessSessionResult.Failure(
                "ProviderAccessNotConfigured",
                "Provider access shared secret is not configured.",
                StatusCodes.Status503ServiceUnavailable);
        }

        if (GetActiveSessionSigningSecret() is null)
        {
            return ProviderAccessSessionResult.Failure(
                "ProviderAccessNotConfigured",
                "Provider access session signing is not configured.",
                StatusCodes.Status503ServiceUnavailable);
        }

        if (string.IsNullOrWhiteSpace(sharedSecret)
            || !FixedTimeEquals(sharedSecret.Trim(), _options.SharedSecret.Trim()))
        {
            return ProviderAccessSessionResult.Failure(
                "ProviderAccessDenied",
                "Provider access is required before creating a provider session.",
                StatusCodes.Status401Unauthorized);
        }

        var requestedScopes = ProviderAccessScopes.Normalize(scopes, _options.DefaultScopes);
        var unsupportedScopes = ProviderAccessScopes.FindUnsupported(requestedScopes);

        if (unsupportedScopes.Count > 0)
        {
            return ProviderAccessSessionResult.Failure(
                "ProviderAccessScopeUnsupported",
                FormatUnsupportedScopes(unsupportedScopes),
                StatusCodes.Status400BadRequest);
        }

        return CreateSignedSession(
            NormalizeActor(actor),
            requestedScopes,
            expiresInMinutes);
    }

    public async Task<ProviderAccessSessionResult> CreateSessionFromCredentialsAsync(
        string? email,
        string? password,
        IEnumerable<string>? scopes,
        int? expiresInMinutes,
        string? recoveryCode = null,
        string? totpCode = null,
        CancellationToken cancellationToken = default)
    {
        if (GetActiveSessionSigningSecret() is null)
        {
            return ProviderAccessSessionResult.Failure(
                "ProviderAccessNotConfigured",
                "Provider access session signing is not configured.",
                StatusCodes.Status503ServiceUnavailable);
        }

        var normalizedEmail = NormalizeEmail(email);
        var user = await _operators.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null
            || !user.Status.Equals("Active", StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(user.PasswordHash)
            || string.IsNullOrWhiteSpace(password)
            || IsLockedOut(user))
        {
            return user is not null && IsLockedOut(user)
                ? LockedOut(user.LockoutEndsAtUtc!.Value)
                : InvalidCredentials();
        }

        if (!_credentials.VerifyPassword(password, user.PasswordHash))
        {
            return await RecordFailedLoginAsync(
                    user,
                    cancellationToken)
                ?? InvalidCredentials();
        }

        var assignedScopes = ProviderAccessScopes.Normalize(user.Scopes, _options.DefaultScopes);
        var requestedScopes = ProviderAccessScopes.Normalize(scopes, assignedScopes);
        var unsupportedAssignedScopes = ProviderAccessScopes.FindUnsupported(assignedScopes);

        if (unsupportedAssignedScopes.Count > 0)
        {
            return ProviderAccessSessionResult.Failure(
                "ProviderAccessOperatorScopesUnsupported",
                "Provider operator has unsupported assigned scopes.",
                StatusCodes.Status403Forbidden);
        }

        var unsupportedRequestedScopes = ProviderAccessScopes.FindUnsupported(requestedScopes);

        if (unsupportedRequestedScopes.Count > 0)
        {
            return ProviderAccessSessionResult.Failure(
                "ProviderAccessScopeUnsupported",
                FormatUnsupportedScopes(unsupportedRequestedScopes),
                StatusCodes.Status400BadRequest);
        }

        if (!requestedScopes.All(scope => HasScope(assignedScopes, scope)))
        {
            return ProviderAccessSessionResult.Failure(
                "ProviderAccessScopeDenied",
                "Provider operator is not allowed to request one or more scopes.",
                StatusCodes.Status403Forbidden);
        }

        var mfaResult = TryValidateMfa(user, recoveryCode, totpCode);

        if (mfaResult is not null)
        {
            return ShouldCountMfaFailure(mfaResult)
                ? await RecordFailedLoginAsync(user, cancellationToken) ?? mfaResult
                : mfaResult;
        }

        ClearLoginFailures(user);
        user.LastLoginAtUtc = _clock.UtcNow;
        await _operators.SaveAsync(user, cancellationToken);

        return CreateSignedSession(
            BuildOperatorActor(user),
            requestedScopes,
            expiresInMinutes);
    }

    private ProviderAccessSessionResult? TryValidateMfa(
        ProviderAccessOperator user,
        string? recoveryCode,
        string? totpCode)
    {
        var hasTotp = !string.IsNullOrWhiteSpace(user.TotpSecret);
        var hasRecoveryCodes = user.RecoveryCodesUpdatedAtUtc is not null;
        var recoveryCodeHashes = NormalizeRecoveryCodeHashes(user.RecoveryCodeHashes);

        if (!hasTotp && !hasRecoveryCodes)
        {
            return null;
        }

        if (!hasTotp && recoveryCodeHashes.Length == 0)
        {
            return ProviderAccessSessionResult.Failure(
                "ProviderMfaUnavailable",
                "Provider operator has no remaining recovery codes.",
                StatusCodes.Status401Unauthorized);
        }

        var suppliedTotpCode = !string.IsNullOrWhiteSpace(totpCode);
        var invalidTotpCode = false;

        if (hasTotp && suppliedTotpCode)
        {
            if (!TryGetTotpSecret(user, out var totpSecret, out var shouldProtectTotpSecret))
            {
                return ProviderAccessSessionResult.Failure(
                    "ProviderMfaUnavailable",
                    "Provider operator MFA secret is unavailable.",
                    StatusCodes.Status401Unauthorized);
            }

            if (ProviderAccessTotp.TryVerifyCode(
                totpSecret,
                totpCode,
                _clock.UtcNow,
                user.LastTotpStep,
                out var acceptedStep))
            {
                user.LastTotpUsedAtUtc = _clock.UtcNow;
                user.LastTotpStep = acceptedStep;

                if (shouldProtectTotpSecret)
                {
                    user.TotpSecret = _totpSecrets.Protect(totpSecret);
                }

                return null;
            }

            invalidTotpCode = true;
        }

        var normalizedCode = NormalizeRecoveryCode(recoveryCode);

        if (!string.IsNullOrWhiteSpace(normalizedCode))
        {
            var recoveryCodeResult = TryConsumeRecoveryCode(user, recoveryCodeHashes, normalizedCode);

            if (recoveryCodeResult is null)
            {
                return null;
            }

            return hasRecoveryCodes ? recoveryCodeResult : InvalidMfaCode();
        }

        if (invalidTotpCode)
        {
            return InvalidMfaCode();
        }

        if (hasTotp)
        {
            return ProviderAccessSessionResult.Failure(
                "ProviderMfaRequired",
                "Provider operator MFA code is required.",
                StatusCodes.Status401Unauthorized);
        }

        if (recoveryCodeHashes.Length == 0)
        {
            return ProviderAccessSessionResult.Failure(
                "ProviderMfaUnavailable",
                "Provider operator has no remaining recovery codes.",
                StatusCodes.Status401Unauthorized);
        }

        return ProviderAccessSessionResult.Failure(
            "ProviderMfaRequired",
            "Provider operator recovery code is required.",
            StatusCodes.Status401Unauthorized);
    }

    private bool TryGetTotpSecret(
        ProviderAccessOperator user,
        out string secret,
        out bool shouldProtect)
    {
        secret = "";
        shouldProtect = false;

        if (!_totpSecrets.TryUnprotect(user.TotpSecret ?? "", out secret))
        {
            return false;
        }

        shouldProtect = !_totpSecrets.IsProtected(user.TotpSecret ?? "");
        return true;
    }

    private ProviderAccessSessionResult? TryConsumeRecoveryCode(
        ProviderAccessOperator user,
        string[] recoveryCodeHashes,
        string normalizedCode)
    {
        var recoveryCodeHash = HashRecoveryCode(normalizedCode);
        var matchedHash = recoveryCodeHashes.FirstOrDefault(hash =>
            FixedTimeEquals(hash, recoveryCodeHash));

        if (matchedHash is null)
        {
            return InvalidMfaCode();
        }

        user.RecoveryCodeHashes = recoveryCodeHashes
            .Where(hash => !hash.Equals(matchedHash, StringComparison.Ordinal))
            .ToArray();
        user.LastRecoveryCodeUsedAtUtc = _clock.UtcNow;

        return null;
    }

    private static ProviderAccessSessionResult InvalidMfaCode()
    {
        return ProviderAccessSessionResult.Failure(
            "ProviderMfaInvalid",
            "Provider operator MFA code is invalid.",
            StatusCodes.Status401Unauthorized);
    }

    private async Task<ProviderAccessSessionResult?> RecordFailedLoginAsync(
        ProviderAccessOperator user,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var failedAttemptCount = user.LockoutEndsAtUtc is not null && user.LockoutEndsAtUtc <= now
            ? 0
            : Math.Max(0, user.FailedLoginAttemptCount);

        user.FailedLoginAttemptCount = failedAttemptCount + 1;
        user.LastFailedLoginAtUtc = now;

        if (user.FailedLoginAttemptCount >= GetLockoutThreshold())
        {
            user.LockoutEndsAtUtc = now.AddMinutes(GetLockoutMinutes());
        }

        await _operators.SaveAsync(user, cancellationToken);

        return IsLockedOut(user)
            ? LockedOut(user.LockoutEndsAtUtc!.Value)
            : null;
    }

    private void ClearLoginFailures(ProviderAccessOperator user)
    {
        user.FailedLoginAttemptCount = 0;
        user.LastFailedLoginAtUtc = null;
        user.LockoutEndsAtUtc = null;
    }

    private bool IsLockedOut(ProviderAccessOperator user)
    {
        return user.LockoutEndsAtUtc is not null
            && user.LockoutEndsAtUtc > _clock.UtcNow;
    }

    private int GetLockoutThreshold()
    {
        return Math.Clamp(
            _options.FailedLoginLockoutThreshold,
            MinLockoutThreshold,
            MaxLockoutThreshold);
    }

    private int GetLockoutMinutes()
    {
        return Math.Clamp(
            _options.FailedLoginLockoutMinutes,
            MinLockoutMinutes,
            MaxLockoutMinutes);
    }

    private static bool ShouldCountMfaFailure(ProviderAccessSessionResult result)
    {
        return result.FailureCode is "ProviderMfaRequired" or "ProviderMfaInvalid";
    }

    private static ProviderAccessSessionResult InvalidCredentials()
    {
        return ProviderAccessSessionResult.Failure(
            "ProviderCredentialsInvalid",
            "Provider operator credentials are invalid.",
            StatusCodes.Status401Unauthorized);
    }

    private static ProviderAccessSessionResult LockedOut(DateTimeOffset lockoutEndsAtUtc)
    {
        return ProviderAccessSessionResult.Failure(
            "ProviderLoginLocked",
            $"Provider operator login is temporarily locked until {lockoutEndsAtUtc:O}.",
            StatusCodes.Status423Locked);
    }

    public ProviderAccessAuthorizationResult Authorize(
        HttpRequest request,
        string requiredScope)
    {
        var tokenResult = ValidateBearerToken(request.Headers.Authorization.ToString(), requiredScope);

        if (tokenResult is not null)
        {
            return tokenResult;
        }

        return ValidateSharedSecretFallback(request, requiredScope);
    }

    private ProviderAccessAuthorizationResult? ValidateBearerToken(
        string authorizationHeader,
        string requiredScope)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader)
            || !authorizationHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!GetSessionValidationSecrets().Any())
        {
            return ProviderAccessAuthorizationResult.Failure(
                "ProviderAccessNotConfigured",
                "Provider access session signing is not configured.",
                StatusCodes.Status503ServiceUnavailable);
        }

        var token = authorizationHeader[BearerPrefix.Length..].Trim();
        var parts = token.Split('.', 2);

        if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
        {
            return ProviderAccessAuthorizationResult.Failure(
                "ProviderAccessDenied",
                "Provider access token is invalid.",
                StatusCodes.Status401Unauthorized);
        }

        if (!HasValidSignature(parts[0], parts[1]))
        {
            return ProviderAccessAuthorizationResult.Failure(
                "ProviderAccessDenied",
                "Provider access token signature is invalid.",
                StatusCodes.Status401Unauthorized);
        }

        ProviderAccessTokenPayload? payload;

        try
        {
            payload = JsonSerializer.Deserialize<ProviderAccessTokenPayload>(
                Base64UrlDecode(parts[0]),
                JsonOptions);
        }
        catch (FormatException)
        {
            return ProviderAccessAuthorizationResult.Failure(
                "ProviderAccessDenied",
                "Provider access token payload is invalid.",
                StatusCodes.Status401Unauthorized);
        }
        catch (JsonException)
        {
            return ProviderAccessAuthorizationResult.Failure(
                "ProviderAccessDenied",
                "Provider access token payload is invalid.",
                StatusCodes.Status401Unauthorized);
        }

        if (payload is null
            || !payload.Type.Equals(TokenType, StringComparison.Ordinal)
            || !payload.Issuer.Equals(Issuer, StringComparison.Ordinal)
            || payload.SessionId == Guid.Empty
            || string.IsNullOrWhiteSpace(payload.Actor))
        {
            return ProviderAccessAuthorizationResult.Failure(
                "ProviderAccessDenied",
                "Provider access token payload is invalid.",
                StatusCodes.Status401Unauthorized);
        }

        if (payload.ExpiresAtUtc <= _clock.UtcNow)
        {
            return ProviderAccessAuthorizationResult.Failure(
                "ProviderAccessExpired",
                "Provider access token has expired.",
                StatusCodes.Status401Unauthorized);
        }

        if (!HasScope(payload.Scopes, requiredScope))
        {
            return ProviderAccessAuthorizationResult.Failure(
                "ProviderAccessScopeDenied",
                "Provider session is not allowed to perform this action.",
                StatusCodes.Status403Forbidden);
        }

        return ProviderAccessAuthorizationResult.Success(
            new ProviderAccessPrincipal(
                payload.Actor.Trim(),
                "BearerSession",
                payload.Scopes,
                payload.ExpiresAtUtc));
    }

    private ProviderAccessAuthorizationResult ValidateSharedSecretFallback(
        HttpRequest request,
        string requiredScope)
    {
        var expectedSecret = _options.SharedSecret.Trim();

        if (string.IsNullOrWhiteSpace(expectedSecret))
        {
            return ProviderAccessAuthorizationResult.Failure(
                "ProviderAccessNotConfigured",
                "Provider access is not configured.",
                StatusCodes.Status503ServiceUnavailable);
        }

        var providedSecret = request.Headers[ProviderAccessHeaderName].ToString().Trim();

        if (string.IsNullOrWhiteSpace(providedSecret)
            || !FixedTimeEquals(providedSecret, expectedSecret))
        {
            return ProviderAccessAuthorizationResult.Failure(
                "ProviderAccessDenied",
                "Provider access is required before this cloud action.",
                StatusCodes.Status401Unauthorized);
        }

        return ProviderAccessAuthorizationResult.Success(
            new ProviderAccessPrincipal(
                "provider-shared-secret",
                "SharedSecret",
                ProviderAccessScopes.Normalize([requiredScope], _options.DefaultScopes),
                DateTimeOffset.MaxValue));
    }

    private ProviderAccessSessionResult CreateSignedSession(
        string actor,
        IReadOnlyCollection<string> scopes,
        int? expiresInMinutes)
    {
        var now = _clock.UtcNow;
        var sessionMinutes = expiresInMinutes is null
            ? Math.Clamp(_options.SessionMinutes, 5, 1440)
            : Math.Clamp(expiresInMinutes.Value, 5, 1440);
        var expiresAtUtc = now.AddMinutes(sessionMinutes);
        var payload = new ProviderAccessTokenPayload(
            TokenType,
            Issuer,
            Guid.NewGuid(),
            actor,
            scopes.ToArray(),
            now,
            expiresAtUtc);
        var payloadText = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions));
        var signingSecret = GetActiveSessionSigningSecret();

        if (signingSecret is null)
        {
            return ProviderAccessSessionResult.Failure(
                "ProviderAccessNotConfigured",
                "Provider access session signing is not configured.",
                StatusCodes.Status503ServiceUnavailable);
        }

        var signature = Sign(payloadText, signingSecret);

        return ProviderAccessSessionResult.Success(
            $"{payloadText}.{signature}",
            actor,
            scopes,
            expiresAtUtc);
    }

    private string? GetActiveSessionSigningSecret()
    {
        var signingKeys = GetConfiguredSessionSigningKeys().ToArray();

        if (signingKeys.Length == 0)
        {
            return string.IsNullOrWhiteSpace(_options.SessionSigningSecret)
                ? null
                : _options.SessionSigningSecret;
        }

        var activeKeyId = _options.ActiveSessionSigningKeyId?.Trim();

        if (!string.IsNullOrWhiteSpace(activeKeyId))
        {
            return signingKeys
                .FirstOrDefault(key => key.KeyId.Trim().Equals(activeKeyId, StringComparison.Ordinal))
                ?.Secret;
        }

        return signingKeys.Length == 1
            ? signingKeys[0].Secret
            : null;
    }

    private bool HasValidSignature(string payloadText, string signature)
    {
        foreach (var secret in GetSessionValidationSecrets())
        {
            if (FixedTimeEquals(signature, Sign(payloadText, secret)))
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerable<string> GetSessionValidationSecrets()
    {
        var seenSecrets = new HashSet<string>(StringComparer.Ordinal);

        foreach (var key in GetConfiguredSessionSigningKeys())
        {
            if (seenSecrets.Add(key.Secret))
            {
                yield return key.Secret;
            }
        }

        if (!string.IsNullOrWhiteSpace(_options.SessionSigningSecret)
            && seenSecrets.Add(_options.SessionSigningSecret))
        {
            yield return _options.SessionSigningSecret;
        }
    }

    private IEnumerable<ProviderAccessSessionSigningKeyOptions> GetConfiguredSessionSigningKeys()
    {
        foreach (var key in _options.SessionSigningKeys ?? [])
        {
            if (key is not null
                && !string.IsNullOrWhiteSpace(key.KeyId)
                && !string.IsNullOrWhiteSpace(key.Secret))
            {
                yield return key;
            }
        }
    }

    private static string Sign(string payloadText, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadText));

        return Base64UrlEncode(signature);
    }

    private static bool HasScope(
        IEnumerable<string> scopes,
        string requiredScope)
    {
        return scopes.Any(scope =>
            scope.Equals(ProviderAccessScopes.Any, StringComparison.OrdinalIgnoreCase)
            || scope.Equals(requiredScope, StringComparison.OrdinalIgnoreCase));
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string NormalizeActor(string? actor)
    {
        var normalized = actor?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? "SafarSuite Control Desk"
            : normalized;
    }

    private static string NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? ""
            : email.Trim().ToLowerInvariant();
    }

    private static string BuildOperatorActor(ProviderAccessOperator user)
    {
        var fullName = user.FullName.Trim();

        return string.IsNullOrWhiteSpace(fullName)
            ? NormalizeEmail(user.Email)
            : fullName;
    }

    private string HashRecoveryCode(string recoveryCode)
    {
        return _credentials.HashSecret($"provider-access-recovery-code:{recoveryCode}");
    }

    private static string NormalizeRecoveryCode(string? recoveryCode)
    {
        if (string.IsNullOrWhiteSpace(recoveryCode))
        {
            return "";
        }

        return new string(recoveryCode
            .Where(character => !char.IsWhiteSpace(character) && character != '-')
            .Select(character => char.ToUpperInvariant(character))
            .ToArray());
    }

    private static string[] NormalizeRecoveryCodeHashes(IEnumerable<string>? recoveryCodeHashes)
    {
        return (recoveryCodeHashes ?? [])
            .Select(hash => hash.Trim())
            .Where(hash => !string.IsNullOrWhiteSpace(hash))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string FormatUnsupportedScopes(IReadOnlyCollection<string> scopes)
    {
        return $"Unsupported provider access scope(s): {string.Join(", ", scopes)}.";
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

    private sealed record ProviderAccessTokenPayload(
        string Type,
        string Issuer,
        Guid SessionId,
        string Actor,
        string[] Scopes,
        DateTimeOffset IssuedAtUtc,
        DateTimeOffset ExpiresAtUtc);
}
