using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Pairing.Ports;
using SafarSuite.LocalServer.Application.Registration.Ports;
using SafarSuite.LocalServer.Domain.Pairing;
using SafarSuite.LocalServer.Domain.Registration;

namespace SafarSuite.LocalServer.Api.Modules.LocalServer;

public sealed class LocalServerManagerSessionService
{
    private const string BearerPrefix = "Bearer ";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly LocalServerManagerSessionOptions _options;

    public LocalServerManagerSessionService(
        LocalServerManagerSessionOptions options)
    {
        _options = options;
    }

    public LocalServerManagerSessionIssueResult Issue(
        LocalServerBootstrapConfiguration configuration,
        LocalServerDevicePairingRecord device,
        string? requestedBy,
        DateTimeOffset issuedAtUtc)
    {
        if (!IsConfigured)
        {
            return LocalServerManagerSessionIssueResult.Failure(
                "ManagerSessionSigningNotConfigured",
                "Local manager session signing is not configured.");
        }

        if (!IsApprovedManagerDevice(device))
        {
            return LocalServerManagerSessionIssueResult.Failure(
                "ManagerDeviceNotApproved",
                "Only approved local manager devices can create manager sessions.");
        }

        var deviceCredentialId = NormalizeRequired(device.DeviceCredentialId, 120);
        if (deviceCredentialId is null)
        {
            return LocalServerManagerSessionIssueResult.Failure(
                "ManagerDeviceCredentialInvalid",
                "Approved manager device does not have an active device credential.");
        }

        var expiresAtUtc = issuedAtUtc.AddMinutes(_options.SessionMinutes);
        var sessionId = Guid.NewGuid();
        var actor = BuildActor(device, requestedBy);
        var payload = new LocalServerManagerSessionPayload(
            LocalServerPairingFormats.ManagerSessionVersion,
            sessionId,
            configuration.ClientId,
            configuration.InstallationId,
            device.DeviceId,
            deviceCredentialId,
            actor,
            NormalizeRequired(device.AssignedRole, 80),
            _options.SigningKeyId,
            issuedAtUtc,
            expiresAtUtc);
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
        var signature = Sign(payloadJson);
        var token = string.Concat(
            Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson)),
            ".",
            Base64UrlEncode(signature));

        return LocalServerManagerSessionIssueResult.Success(
            new LocalServerManagerSessionResponse(
                "Bearer",
                token,
                sessionId,
                configuration.ClientId,
                configuration.InstallationId,
                device.DeviceId,
                actor,
                payload.AssignedRole,
                _options.SigningKeyId,
                issuedAtUtc,
                expiresAtUtc));
    }

    public async Task<LocalServerManagerSessionValidationResult> ValidateAsync(
        HttpRequest request,
        ILocalServerBootstrapConfigurationStore configurationStore,
        ILocalServerDevicePairingStore pairingStore,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return LocalServerManagerSessionValidationResult.Failure(
                "ManagerSessionSigningNotConfigured",
                "Local manager session signing is not configured.");
        }

        var token = ExtractBearerToken(request);
        if (token is null)
        {
            return LocalServerManagerSessionValidationResult.Failure(
                "ManagerSessionRequired",
                "A local manager bearer session is required before managing paired devices.");
        }

        if (!TryReadPayload(token, out var payload))
        {
            return LocalServerManagerSessionValidationResult.Failure(
                "ManagerSessionInvalid",
                "Local manager session token is invalid.");
        }

        if (!string.Equals(
                payload.FormatVersion,
                LocalServerPairingFormats.ManagerSessionVersion,
                StringComparison.Ordinal)
            || !string.Equals(payload.SigningKeyId, _options.SigningKeyId, StringComparison.Ordinal))
        {
            return LocalServerManagerSessionValidationResult.Failure(
                "ManagerSessionInvalid",
                "Local manager session token is invalid.");
        }

        var now = DateTimeOffset.UtcNow;
        if (payload.ExpiresAtUtc <= now)
        {
            return LocalServerManagerSessionValidationResult.Failure(
                "ManagerSessionExpired",
                "Local manager session has expired.");
        }

        var configuration = await configurationStore.GetCurrentAsync(cancellationToken);
        if (configuration is null)
        {
            return LocalServerManagerSessionValidationResult.Failure(
                "BootstrapConfigurationMissing",
                "A verified bootstrap configuration is required before managing paired devices.");
        }

        if (payload.ClientId != configuration.ClientId
            || !string.Equals(payload.InstallationId, configuration.InstallationId, StringComparison.Ordinal))
        {
            return LocalServerManagerSessionValidationResult.Failure(
                "ManagerSessionScopeMismatch",
                "Local manager session belongs to another client or LocalServer installation.");
        }

        var device = await pairingStore.GetByDeviceIdAsync(payload.DeviceId, cancellationToken);
        if (device is null
            || device.ClientId != configuration.ClientId
            || !string.Equals(device.InstallationId, configuration.InstallationId, StringComparison.Ordinal)
            || !string.Equals(device.DeviceCredentialId, payload.DeviceCredentialId, StringComparison.Ordinal))
        {
            return LocalServerManagerSessionValidationResult.Failure(
                "ManagerSessionDeviceInvalid",
                "Local manager session is no longer valid for this device.");
        }

        if (!IsApprovedManagerDevice(device))
        {
            return LocalServerManagerSessionValidationResult.Failure(
                "ManagerSessionDeviceNotApproved",
                "Local manager session device is not approved.");
        }

        return LocalServerManagerSessionValidationResult.Success(payload.Actor, device);
    }

    private bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.SigningKeyId)
        && !string.IsNullOrWhiteSpace(_options.SigningSecret);

    private bool TryReadPayload(
        string token,
        out LocalServerManagerSessionPayload payload)
    {
        payload = LocalServerManagerSessionPayload.Empty;

        var separatorIndex = token.IndexOf('.', StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex == token.Length - 1)
        {
            return false;
        }

        try
        {
            var payloadSegment = token[..separatorIndex];
            var signatureSegment = token[(separatorIndex + 1)..];
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(payloadSegment));
            var providedSignature = Base64UrlDecode(signatureSegment);
            var expectedSignature = Sign(payloadJson);

            if (!FixedTimeEquals(providedSignature, expectedSignature))
            {
                return false;
            }

            payload = JsonSerializer.Deserialize<LocalServerManagerSessionPayload>(
                payloadJson,
                JsonOptions) ?? LocalServerManagerSessionPayload.Empty;

            return payload.SessionId != Guid.Empty
                && payload.ClientId != Guid.Empty
                && payload.DeviceId != Guid.Empty
                && !string.IsNullOrWhiteSpace(payload.InstallationId)
                && !string.IsNullOrWhiteSpace(payload.Actor)
                && !string.IsNullOrWhiteSpace(payload.DeviceCredentialId);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private byte[] Sign(string payloadJson)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SigningSecret));

        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadJson));
    }

    private static string? ExtractBearerToken(HttpRequest request)
    {
        var authorization = request.Headers.Authorization.ToString().Trim();

        if (authorization.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var token = authorization[BearerPrefix.Length..].Trim();

            return string.IsNullOrWhiteSpace(token) ? null : token;
        }

        return null;
    }

    private static bool IsApprovedManagerDevice(LocalServerDevicePairingRecord device)
    {
        if (!string.Equals(
                device.DeviceStatus,
                LocalServerDevicePairingRecordStatuses.Approved,
                StringComparison.Ordinal))
        {
            return false;
        }

        return string.Equals(device.AssignedRole, "FirstManagerDevice", StringComparison.Ordinal)
            || string.Equals(device.AssignedRole, "ManagerApprovedDevice", StringComparison.Ordinal);
    }

    private static string BuildActor(
        LocalServerDevicePairingRecord device,
        string? requestedBy)
    {
        var deviceName = NormalizeRequired(device.DeviceDisplayName, 80)
            ?? device.DeviceId.ToString("D");
        var userHint = NormalizeRequired(requestedBy, 80);

        return userHint is null
            ? $"manager-device:{deviceName}"
            : $"manager-device:{deviceName}:{userHint}";
    }

    private static bool FixedTimeEquals(byte[] left, byte[] right)
    {
        return left.Length == right.Length
            && CryptographicOperations.FixedTimeEquals(left, right);
    }

    private static string? NormalizeRequired(
        string? value,
        int maxLength)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
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
        var base64 = value
            .Replace('-', '+')
            .Replace('_', '/');

        base64 = (base64.Length % 4) switch
        {
            2 => base64 + "==",
            3 => base64 + "=",
            _ => base64
        };

        return Convert.FromBase64String(base64);
    }

    private sealed record LocalServerManagerSessionPayload(
        string FormatVersion,
        Guid SessionId,
        Guid ClientId,
        string InstallationId,
        Guid DeviceId,
        string DeviceCredentialId,
        string Actor,
        string? AssignedRole,
        string SigningKeyId,
        DateTimeOffset IssuedAtUtc,
        DateTimeOffset ExpiresAtUtc)
    {
        public static LocalServerManagerSessionPayload Empty { get; } = new(
            string.Empty,
            Guid.Empty,
            Guid.Empty,
            string.Empty,
            Guid.Empty,
            string.Empty,
            string.Empty,
            null,
            string.Empty,
            DateTimeOffset.MinValue,
            DateTimeOffset.MinValue);
    }
}

public sealed record LocalServerManagerSessionIssueResult(
    LocalServerManagerSessionResponse? Session,
    string? FailureCode,
    string? Detail)
{
    public bool IsSuccess => Session is not null;

    public static LocalServerManagerSessionIssueResult Success(
        LocalServerManagerSessionResponse session)
    {
        return new LocalServerManagerSessionIssueResult(session, null, null);
    }

    public static LocalServerManagerSessionIssueResult Failure(
        string failureCode,
        string detail)
    {
        return new LocalServerManagerSessionIssueResult(null, failureCode, detail);
    }
}

public sealed record LocalServerManagerSessionValidationResult(
    LocalServerDevicePairingRecord? Device,
    string? Actor,
    string? FailureCode,
    string? Detail)
{
    public bool IsSuccess => Device is not null && !string.IsNullOrWhiteSpace(Actor);

    public static LocalServerManagerSessionValidationResult Success(
        string actor,
        LocalServerDevicePairingRecord device)
    {
        return new LocalServerManagerSessionValidationResult(
            device,
            actor,
            null,
            null);
    }

    public static LocalServerManagerSessionValidationResult Failure(
        string failureCode,
        string detail)
    {
        return new LocalServerManagerSessionValidationResult(null, null, failureCode, detail);
    }
}
