using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SafarSuite.ControlCloud.Application.Modules.InboundControlDesk.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.InboundControlDesk;

public sealed class ControlCloudEnvelopeSignatureValidator
{
    private const string ExpectedEnvelopeVersion = "1";
    private const string ExpectedSignatureAlgorithm = "HMAC-SHA256";

    private readonly IControlCloudSigningKeyStore _signingKeys;

    public ControlCloudEnvelopeSignatureValidator(IControlCloudSigningKeyStore signingKeys)
    {
        _signingKeys = signingKeys;
    }

    public ControlCloudEnvelopeValidationResult Validate(ControlCloudEnvelope envelope)
    {
        if (envelope.EnvelopeVersion != ExpectedEnvelopeVersion)
        {
            return ControlCloudEnvelopeValidationResult.Invalid(
                "EnvelopeVersionUnsupported",
                $"Envelope version '{envelope.EnvelopeVersion}' is not supported.");
        }

        if (envelope.MessageId == Guid.Empty)
        {
            return ControlCloudEnvelopeValidationResult.Invalid(
                "MessageIdRequired",
                "Message id is required.");
        }

        if (string.IsNullOrWhiteSpace(envelope.MessageType)
            || string.IsNullOrWhiteSpace(envelope.SubjectType)
            || string.IsNullOrWhiteSpace(envelope.SubjectId)
            || string.IsNullOrWhiteSpace(envelope.SourceSystem)
            || string.IsNullOrWhiteSpace(envelope.SourceEnvironment)
            || string.IsNullOrWhiteSpace(envelope.IdempotencyKey))
        {
            return ControlCloudEnvelopeValidationResult.Invalid(
                "EnvelopeFieldRequired",
                "Envelope message, subject, source, and idempotency fields are required.");
        }

        if (envelope.Payload.ValueKind is System.Text.Json.JsonValueKind.Undefined
            or System.Text.Json.JsonValueKind.Null)
        {
            return ControlCloudEnvelopeValidationResult.Invalid(
                "PayloadRequired",
                "Envelope payload is required.");
        }

        if (envelope.Signature is null)
        {
            return ControlCloudEnvelopeValidationResult.Invalid(
                "SignatureRequired",
                "Envelope signature is required.");
        }

        if (envelope.Signature.Algorithm != ExpectedSignatureAlgorithm)
        {
            return ControlCloudEnvelopeValidationResult.Invalid(
                "SignatureAlgorithmUnsupported",
                $"Signature algorithm '{envelope.Signature.Algorithm}' is not supported.");
        }

        if (string.IsNullOrWhiteSpace(envelope.Signature.KeyId)
            || string.IsNullOrWhiteSpace(envelope.Signature.PayloadSha256)
            || string.IsNullOrWhiteSpace(envelope.Signature.Value))
        {
            return ControlCloudEnvelopeValidationResult.Invalid(
                "SignatureFieldRequired",
                "Signature key id, payload hash, and value are required.");
        }

        if (!_signingKeys.TryGetSecret(envelope.Signature.KeyId, out var signingSecret))
        {
            return ControlCloudEnvelopeValidationResult.Invalid(
                "SigningKeyUnknown",
                $"Signing key '{envelope.Signature.KeyId}' is not configured.");
        }

        var payloadSha256 = ComputeSha256(envelope.Payload.GetRawText());

        if (!string.Equals(payloadSha256, envelope.Signature.PayloadSha256, StringComparison.Ordinal))
        {
            return ControlCloudEnvelopeValidationResult.Invalid(
                "PayloadHashMismatch",
                "Envelope payload hash does not match the payload body.");
        }

        var expectedSignature = Sign(
            signingSecret,
            BuildSignatureInput(envelope, payloadSha256));

        if (!FixedTimeEqualsBase64(expectedSignature, envelope.Signature.Value))
        {
            return ControlCloudEnvelopeValidationResult.Invalid(
                "SignatureInvalid",
                "Envelope signature is not valid.");
        }

        return ControlCloudEnvelopeValidationResult.Valid();
    }

    private static string BuildSignatureInput(ControlCloudEnvelope envelope, string payloadSha256)
    {
        return string.Join(
            "\n",
            ExpectedEnvelopeVersion,
            envelope.MessageId.ToString("D", CultureInfo.InvariantCulture),
            envelope.MessageType,
            envelope.SubjectType,
            envelope.SubjectId,
            envelope.SourceSystem,
            envelope.SourceEnvironment,
            envelope.OccurredAtUtc.ToString("O", CultureInfo.InvariantCulture),
            envelope.PreparedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            envelope.IdempotencyKey,
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
