using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Pairing.Ports;
using SafarSuite.LocalServer.Infrastructure.Registration;

namespace SafarSuite.LocalServer.Infrastructure.Pairing;

public sealed class HmacLocalServerFirstManagerSetupTokenVerifier
    : ILocalServerFirstManagerSetupTokenVerifier
{
    private const string ExpectedSignatureAlgorithm = "HMAC-SHA256";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IReadOnlyDictionary<string, string> _secretsByKeyId;

    public HmacLocalServerFirstManagerSetupTokenVerifier(
        LocalServerBootstrapTrustOptions options)
    {
        _secretsByKeyId = options.SigningKeys
            .Where(key => !string.IsNullOrWhiteSpace(key.KeyId) && !string.IsNullOrWhiteSpace(key.Secret))
            .GroupBy(key => key.KeyId.Trim(), StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.First().Secret.Trim(),
                StringComparer.Ordinal);
    }

    public LocalServerFirstManagerSetupTokenVerificationResult Verify(
        LocalServerSignedFirstManagerSetupTokenResponse? token,
        DateTimeOffset importedAtUtc)
    {
        if (token is null)
        {
            return Failure(
                "FirstManagerSetupTokenRequired",
                "First-manager setup token is required.");
        }

        if (string.IsNullOrWhiteSpace(token.PayloadJson))
        {
            return Failure(
                "PayloadRequired",
                "First-manager setup token payload JSON is required.");
        }

        if (token.Signature is null)
        {
            return Failure(
                "SignatureRequired",
                "First-manager setup token signature is required.");
        }

        if (string.IsNullOrWhiteSpace(token.Signature.KeyId))
        {
            return Failure(
                "SigningKeyRequired",
                "First-manager setup token signing key id is required.");
        }

        if (string.IsNullOrWhiteSpace(token.Signature.PayloadSha256))
        {
            return Failure(
                "PayloadHashRequired",
                "First-manager setup token payload hash is required.");
        }

        if (string.IsNullOrWhiteSpace(token.Signature.Value))
        {
            return Failure(
                "SignatureValueRequired",
                "First-manager setup token signature value is required.");
        }

        if (!string.Equals(token.Signature.Algorithm, ExpectedSignatureAlgorithm, StringComparison.Ordinal))
        {
            return Failure(
                "SignatureAlgorithmUnsupported",
                $"Signature algorithm '{token.Signature.Algorithm}' is not supported.");
        }

        if (!_secretsByKeyId.TryGetValue(token.Signature.KeyId.Trim(), out var signingSecret))
        {
            return Failure(
                "SigningKeyUnknown",
                $"Signing key '{token.Signature.KeyId}' is not trusted by this local server.");
        }

        var payloadSha256 = ComputeSha256(token.PayloadJson);

        if (!string.Equals(payloadSha256, token.Signature.PayloadSha256, StringComparison.Ordinal))
        {
            return Failure(
                "PayloadHashMismatch",
                "First-manager setup token payload hash does not match the signed payload.");
        }

        var expectedSignature = Sign(signingSecret, token.PayloadJson);

        if (!FixedTimeEqualsBase64(expectedSignature, token.Signature.Value))
        {
            return Failure(
                "SignatureInvalid",
                "First-manager setup token signature is not valid.");
        }

        LocalServerFirstManagerSetupTokenPayloadResponse payload;

        try
        {
            payload = JsonSerializer.Deserialize<LocalServerFirstManagerSetupTokenPayloadResponse>(
                token.PayloadJson,
                JsonOptions) ?? throw new JsonException("Payload JSON was empty.");
        }
        catch (JsonException exception)
        {
            return Failure(
                "PayloadInvalid",
                $"First-manager setup token payload JSON could not be parsed: {exception.Message}");
        }

        if (!string.Equals(
                payload.FormatVersion,
                LocalServerPairingFormats.FirstManagerSetupTokenVersion,
                StringComparison.Ordinal))
        {
            return Failure(
                "FormatVersionUnsupported",
                "First-manager setup token format version is not supported.");
        }

        if (payload.TokenId == Guid.Empty)
        {
            return Failure(
                "TokenIdRequired",
                "First-manager setup token id is required.");
        }

        if (payload.ClientId == Guid.Empty)
        {
            return Failure(
                "ClientIdRequired",
                "First-manager setup token client id is required.");
        }

        if (string.IsNullOrWhiteSpace(payload.InstallationId))
        {
            return Failure(
                "InstallationIdRequired",
                "First-manager setup token installation id is required.");
        }

        if (payload.PendingDeviceRequestId == Guid.Empty)
        {
            return Failure(
                "PendingDeviceRequestIdRequired",
                "First-manager setup token pending device request id is required.");
        }

        if (payload.ExpiresAtUtc <= importedAtUtc)
        {
            return Failure(
                "FirstManagerSetupTokenExpired",
                "First-manager setup token is expired.");
        }

        if (string.IsNullOrWhiteSpace(payload.ManagerDisplayName))
        {
            return Failure(
                "ManagerDisplayNameRequired",
                "First-manager setup token manager display name is required.");
        }

        if (payload.AllowedActions is null || payload.AllowedActions.Count == 0)
        {
            return Failure(
                "FirstManagerSetupTokenActionsRequired",
                "First-manager setup token must include allowed actions.");
        }

        if (!HasAction(payload, LocalServerFirstManagerSetupTokenActions.CreateFirstManager)
            || !HasAction(payload, LocalServerFirstManagerSetupTokenActions.ApproveFirstDevice))
        {
            return Failure(
                "FirstManagerSetupTokenActionsInvalid",
                "First-manager setup token must allow first manager creation and first device approval.");
        }

        return LocalServerFirstManagerSetupTokenVerificationResult.Success(
            payload,
            token.Signature);
    }

    private static bool HasAction(
        LocalServerFirstManagerSetupTokenPayloadResponse payload,
        string action)
    {
        return payload.AllowedActions.Any(
            candidate => string.Equals(candidate, action, StringComparison.Ordinal));
    }

    private static LocalServerFirstManagerSetupTokenVerificationResult Failure(
        string failureCode,
        string detail)
    {
        return LocalServerFirstManagerSetupTokenVerificationResult.Failure(
            failureCode,
            detail);
    }

    private static string ComputeSha256(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Sign(string signingSecret, string payloadJson)
    {
        var secretBytes = Encoding.UTF8.GetBytes(signingSecret);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        var signatureBytes = HMACSHA256.HashData(secretBytes, payloadBytes);

        return Convert.ToBase64String(signatureBytes);
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
