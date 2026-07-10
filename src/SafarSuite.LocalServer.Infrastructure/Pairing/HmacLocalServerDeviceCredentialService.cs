using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Pairing.Ports;
using SafarSuite.LocalServer.Domain.Pairing;

namespace SafarSuite.LocalServer.Infrastructure.Pairing;

public sealed class HmacLocalServerDeviceCredentialService
    : ILocalServerDeviceCredentialService
{
    private const string SignatureAlgorithm = "HMAC-SHA256";
    private const string CompactTokenPrefix = "safarsuite-device-v1";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _signingKeyId;
    private readonly string _signingSecret;
    private readonly int _expiresInDays;

    public HmacLocalServerDeviceCredentialService(
        LocalServerDeviceCredentialOptions options)
    {
        _signingKeyId = options.SigningKeyId.Trim();
        _signingSecret = options.SigningSecret.Trim();
        _expiresInDays = options.ExpiresInDays;

        if (string.IsNullOrWhiteSpace(_signingKeyId))
        {
            throw new InvalidOperationException(
                "DeviceCredentials:SigningKeyId is required for LocalServer device credential signing.");
        }

        if (string.IsNullOrWhiteSpace(_signingSecret))
        {
            throw new InvalidOperationException(
                "DeviceCredentials:SigningSecret is required for LocalServer device credential signing.");
        }
    }

    public LocalServerSignedDeviceCredentialResponse Issue(
        LocalServerDevicePairingRecord device,
        Guid credentialId,
        string assignedRole,
        DateTimeOffset issuedAtUtc)
    {
        var payload = new LocalServerDeviceCredentialPayloadResponse(
            LocalServerPairingFormats.DeviceCredentialVersion,
            credentialId,
            device.ClientId,
            device.InstallationId,
            device.PairingRequestId,
            device.DeviceId,
            device.DevicePublicKeySha256 ?? string.Empty,
            assignedRole.Trim(),
            issuedAtUtc,
            _expiresInDays <= 0
                ? null
                : issuedAtUtc.AddDays(_expiresInDays));
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
        var payloadSha256 = ComputeSha256(payloadJson);
        var signatureValue = Sign(_signingSecret, payloadJson);
        var signature = new LocalServerBootstrapPackageSignatureResponse(
            SignatureAlgorithm,
            _signingKeyId,
            payloadSha256,
            signatureValue);
        var compactToken = CreateCompactToken(
            payloadJson,
            _signingKeyId,
            signatureValue);

        return new LocalServerSignedDeviceCredentialResponse(
            payloadJson,
            payload,
            signature,
            compactToken);
    }

    public LocalServerDeviceCredentialVerificationResult Verify(
        string? compactToken,
        DateTimeOffset verifiedAtUtc)
    {
        var token = compactToken?.Trim();

        if (string.IsNullOrWhiteSpace(token))
        {
            return Failure(
                "DeviceCredentialRequired",
                "A device credential is required.");
        }

        if (!TryReadCompactToken(token, out var payloadJson, out var keyId, out var signatureValue, out var failure))
        {
            return failure!;
        }

        if (!string.Equals(keyId, _signingKeyId, StringComparison.Ordinal))
        {
            return Failure(
                "DeviceCredentialSigningKeyUnknown",
                "Device credential signing key is not trusted by this LocalServer.");
        }

        var expectedSignature = Sign(_signingSecret, payloadJson!);

        if (!FixedTimeEqualsBase64(expectedSignature, signatureValue!))
        {
            return Failure(
                "DeviceCredentialSignatureInvalid",
                "Device credential signature is not valid.");
        }

        LocalServerDeviceCredentialPayloadResponse payload;

        try
        {
            payload = JsonSerializer.Deserialize<LocalServerDeviceCredentialPayloadResponse>(
                payloadJson!,
                JsonOptions) ?? throw new JsonException("Payload JSON was empty.");
        }
        catch (JsonException exception)
        {
            return Failure(
                "DeviceCredentialPayloadInvalid",
                $"Device credential payload JSON could not be parsed: {exception.Message}");
        }

        if (!string.Equals(
                payload.FormatVersion,
                LocalServerPairingFormats.DeviceCredentialVersion,
                StringComparison.Ordinal))
        {
            return Failure(
                "DeviceCredentialFormatUnsupported",
                "Device credential format version is not supported.");
        }

        if (payload.CredentialId == Guid.Empty)
        {
            return Failure(
                "DeviceCredentialIdRequired",
                "Device credential id is required.");
        }

        if (payload.ClientId == Guid.Empty)
        {
            return Failure(
                "ClientIdRequired",
                "Device credential client id is required.");
        }

        if (string.IsNullOrWhiteSpace(payload.InstallationId))
        {
            return Failure(
                "InstallationIdRequired",
                "Device credential installation id is required.");
        }

        if (payload.PairingRequestId == Guid.Empty || payload.DeviceId == Guid.Empty)
        {
            return Failure(
                "DeviceIdRequired",
                "Device credential pairing request id and device id are required.");
        }

        if (payload.ExpiresAtUtc is not null
            && payload.ExpiresAtUtc <= verifiedAtUtc)
        {
            return Failure(
                "DeviceCredentialExpired",
                "Device credential is expired.");
        }

        var signature = new LocalServerBootstrapPackageSignatureResponse(
            SignatureAlgorithm,
            keyId!,
            ComputeSha256(payloadJson!),
            signatureValue!);

        return LocalServerDeviceCredentialVerificationResult.Success(
            payload,
            signature,
            token);
    }

    public LocalServerBootstrapPackageSignatureResponse SignPayloadJson(
        string payloadJson)
    {
        var payload = payloadJson?.Trim();

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException(
                "Payload JSON is required before signing.",
                nameof(payloadJson));
        }

        return new LocalServerBootstrapPackageSignatureResponse(
            SignatureAlgorithm,
            _signingKeyId,
            ComputeSha256(payload),
            Sign(_signingSecret, payload));
    }

    private static LocalServerDeviceCredentialVerificationResult Failure(
        string failureCode,
        string detail)
    {
        return LocalServerDeviceCredentialVerificationResult.Failure(
            failureCode,
            detail);
    }

    private static string CreateCompactToken(
        string payloadJson,
        string keyId,
        string signatureValue)
    {
        return string.Join(
            ".",
            CompactTokenPrefix,
            Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson)),
            Base64UrlEncode(Encoding.UTF8.GetBytes(keyId)),
            Base64UrlEncode(Encoding.UTF8.GetBytes(signatureValue)));
    }

    private static bool TryReadCompactToken(
        string compactToken,
        out string? payloadJson,
        out string? keyId,
        out string? signatureValue,
        out LocalServerDeviceCredentialVerificationResult? failure)
    {
        payloadJson = null;
        keyId = null;
        signatureValue = null;
        failure = null;

        var parts = compactToken.Split('.');

        if (parts.Length != 4
            || !string.Equals(parts[0], CompactTokenPrefix, StringComparison.Ordinal))
        {
            failure = Failure(
                "DeviceCredentialInvalid",
                "Device credential compact token format is invalid.");
            return false;
        }

        try
        {
            payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            keyId = Encoding.UTF8.GetString(Base64UrlDecode(parts[2]));
            signatureValue = Encoding.UTF8.GetString(Base64UrlDecode(parts[3]));
        }
        catch (FormatException)
        {
            failure = Failure(
                "DeviceCredentialInvalid",
                "Device credential compact token encoding is invalid.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(payloadJson)
            || string.IsNullOrWhiteSpace(keyId)
            || string.IsNullOrWhiteSpace(signatureValue))
        {
            failure = Failure(
                "DeviceCredentialInvalid",
                "Device credential compact token is incomplete.");
            return false;
        }

        return true;
    }

    private static string ComputeSha256(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Sign(
        string signingSecret,
        string payloadJson)
    {
        var secretBytes = Encoding.UTF8.GetBytes(signingSecret);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        var signatureBytes = HMACSHA256.HashData(secretBytes, payloadBytes);

        return Convert.ToBase64String(signatureBytes);
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
        var padded = value
            .Replace('-', '+')
            .Replace('_', '/');

        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');

        return Convert.FromBase64String(padded);
    }

    private static bool FixedTimeEqualsBase64(string expected, string actual)
    {
        try
        {
            var expectedBytes = Convert.FromBase64String(expected);
            var actualBytes = Convert.FromBase64String(actual);

            return expectedBytes.Length == actualBytes.Length
                && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
