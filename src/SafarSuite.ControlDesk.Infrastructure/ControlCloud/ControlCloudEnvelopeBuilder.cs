using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;

namespace SafarSuite.ControlDesk.Infrastructure.ControlCloud;

public sealed class ControlCloudEnvelopeBuilder
{
    private const string EnvelopeVersion = "1";
    private const string SignatureAlgorithm = "HMAC-SHA256";

    private readonly IOptions<ControlCloudPublisherOptions> _options;
    private readonly IClock _clock;

    public ControlCloudEnvelopeBuilder(
        IOptions<ControlCloudPublisherOptions> options,
        IClock clock)
    {
        _options = options;
        _clock = clock;
    }

    public ControlCloudEnvelope Build(CloudOutboxMessage message)
    {
        var options = _options.Value;
        var sourceSystem = CleanRequiredText(options.SourceSystem, nameof(options.SourceSystem));
        var sourceEnvironment = CleanRequiredText(options.Environment, nameof(options.Environment));
        var signingKeyId = CleanRequiredText(options.SigningKeyId, nameof(options.SigningKeyId));
        var signingSecret = CleanRequiredText(options.SigningSecret, nameof(options.SigningSecret));
        var preparedAtUtc = _clock.UtcNow;
        var payloadSha256 = ComputeSha256(message.PayloadJson);

        using var payloadDocument = JsonDocument.Parse(message.PayloadJson);
        var payload = payloadDocument.RootElement.Clone();
        var idempotencyKey = $"{sourceSystem}:{message.Id.Value:N}";
        var signatureValue = Sign(
            signingSecret,
            BuildSignatureInput(
                message,
                sourceSystem,
                sourceEnvironment,
                preparedAtUtc,
                idempotencyKey,
                payloadSha256));

        return new ControlCloudEnvelope(
            EnvelopeVersion,
            message.Id.Value,
            message.MessageType,
            message.SubjectType,
            message.SubjectId,
            sourceSystem,
            sourceEnvironment,
            message.OccurredAtUtc,
            preparedAtUtc,
            idempotencyKey,
            payload,
            new ControlCloudEnvelopeSignature(
                SignatureAlgorithm,
                signingKeyId,
                payloadSha256,
                signatureValue));
    }

    private static string BuildSignatureInput(
        CloudOutboxMessage message,
        string sourceSystem,
        string sourceEnvironment,
        DateTimeOffset preparedAtUtc,
        string idempotencyKey,
        string payloadSha256)
    {
        return string.Join(
            "\n",
            EnvelopeVersion,
            message.Id.Value.ToString("D", CultureInfo.InvariantCulture),
            message.MessageType,
            message.SubjectType,
            message.SubjectId,
            sourceSystem,
            sourceEnvironment,
            message.OccurredAtUtc.ToString("O", CultureInfo.InvariantCulture),
            preparedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            idempotencyKey,
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

    private static string CleanRequiredText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{parameterName} is required for Control Cloud publishing.");
        }

        return value.Trim();
    }
}
