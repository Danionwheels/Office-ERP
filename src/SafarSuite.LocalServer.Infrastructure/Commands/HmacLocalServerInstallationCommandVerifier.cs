using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Commands.Ports;
using SafarSuite.LocalServer.Infrastructure.Entitlements;

namespace SafarSuite.LocalServer.Infrastructure.Commands;

public sealed class HmacLocalServerInstallationCommandVerifier
    : ILocalServerInstallationCommandVerifier
{
    private const string ExpectedSignatureAlgorithm = "HMAC-SHA256";
    private const string CommandSignatureVersion = "1";

    private readonly IReadOnlyDictionary<string, string> _secretsByKeyId;

    public HmacLocalServerInstallationCommandVerifier(
        LocalServerEntitlementTrustOptions options)
    {
        _secretsByKeyId = options.SigningKeys
            .Where(key => !string.IsNullOrWhiteSpace(key.KeyId) && !string.IsNullOrWhiteSpace(key.Secret))
            .GroupBy(key => key.KeyId.Trim(), StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.First().Secret.Trim(),
                StringComparer.Ordinal);
    }

    public LocalServerInstallationCommandVerificationResult Verify(
        InstallationCommandResponse command,
        Guid expectedClientId,
        string expectedInstallationId)
    {
        var cleanInstallationId = expectedInstallationId.Trim();

        if (expectedClientId == Guid.Empty)
        {
            return Failure(
                "ClientIdRequired",
                "Expected client id is required before verifying a command.");
        }

        if (cleanInstallationId.Length == 0)
        {
            return Failure(
                "InstallationIdRequired",
                "Expected installation id is required before verifying a command.");
        }

        if (command.ClientId != expectedClientId)
        {
            return Failure(
                "CommandClientMismatch",
                "Command belongs to a different client.");
        }

        if (!string.Equals(command.InstallationId, cleanInstallationId, StringComparison.Ordinal))
        {
            return Failure(
                "CommandInstallationMismatch",
                "Command belongs to a different installation.");
        }

        if (!command.Status.Equals("Pending", StringComparison.Ordinal))
        {
            return Failure(
                "CommandStatusInvalid",
                "Only pending commands can be processed by the local server.");
        }

        if (!command.Signature.Algorithm.Equals(ExpectedSignatureAlgorithm, StringComparison.Ordinal))
        {
            return Failure(
                "SignatureAlgorithmUnsupported",
                $"Command signature algorithm '{command.Signature.Algorithm}' is not supported.");
        }

        if (!_secretsByKeyId.TryGetValue(command.Signature.KeyId.Trim(), out var signingSecret))
        {
            return Failure(
                "SigningKeyUnknown",
                $"Command signing key '{command.Signature.KeyId}' is not trusted by this local server.");
        }

        var payloadJson = command.Payload.GetRawText();
        var payloadSha256 = ComputeSha256(payloadJson);

        if (!string.Equals(payloadSha256, command.Signature.PayloadSha256, StringComparison.Ordinal))
        {
            return Failure(
                "PayloadHashMismatch",
                "Command payload hash does not match the signed payload.");
        }

        var expectedSignature = Sign(
            signingSecret,
            BuildSignatureInput(
                command,
                payloadSha256));

        if (!FixedTimeEqualsBase64(expectedSignature, command.Signature.Value))
        {
            return Failure(
                "SignatureInvalid",
                "Command signature is not valid.");
        }

        return LocalServerInstallationCommandVerificationResult.Success();
    }

    private static LocalServerInstallationCommandVerificationResult Failure(
        string failureCode,
        string detail)
    {
        return LocalServerInstallationCommandVerificationResult.Failure(
            failureCode,
            detail);
    }

    private static string BuildSignatureInput(
        InstallationCommandResponse command,
        string payloadSha256)
    {
        return string.Join(
            "\n",
            CommandSignatureVersion,
            command.CommandId.ToString("D", CultureInfo.InvariantCulture),
            command.ClientId.ToString("D", CultureInfo.InvariantCulture),
            command.InstallationId,
            command.CommandVersion.ToString(CultureInfo.InvariantCulture),
            command.CommandType,
            command.QueuedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            command.NotBeforeUtc?.ToString("O", CultureInfo.InvariantCulture) ?? "",
            command.ExpiresAtUtc.ToString("O", CultureInfo.InvariantCulture),
            payloadSha256);
    }

    private static string ComputeSha256(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Sign(string signingSecret, string signatureInput)
    {
        var secretBytes = Encoding.UTF8.GetBytes(signingSecret);
        var inputBytes = Encoding.UTF8.GetBytes(signatureInput);
        var signatureBytes = HMACSHA256.HashData(secretBytes, inputBytes);

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
