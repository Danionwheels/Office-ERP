using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.ControlCloud;

namespace SafarSuite.ConnectedAcceptance;

internal static class ControlCloudReplayEnvelopeFactory
{
    private const string EnvelopeVersion = "1";
    private const string SignatureAlgorithm = "HMAC-SHA256";
    private const string SourceSystem = "SafarSuite.ControlDesk";

    public static ControlCloudReplayEnvelope Create(
        CloudOutboxMessageResponse message,
        string signingKeyId,
        string signingSecret,
        string sourceEnvironment)
    {
        var cleanSourceEnvironment = string.IsNullOrWhiteSpace(sourceEnvironment)
            ? throw new ConnectedAcceptanceFailureException("Cloud source environment is required for replay signing.")
            : sourceEnvironment.Trim();
        var preparedAtUtc = DateTimeOffset.UtcNow;
        var payload = CreateCanonicalPayload(message.PayloadJson, out var canonicalPayloadJson);
        var payloadSha256 = ComputeSha256(canonicalPayloadJson);
        var idempotencyKey = $"{SourceSystem}:{message.CloudOutboxMessageId:N}";
        var signatureInput = string.Join(
            "\n",
            EnvelopeVersion,
            message.CloudOutboxMessageId.ToString("D", CultureInfo.InvariantCulture),
            message.MessageType,
            message.SubjectType,
            message.SubjectId,
            SourceSystem,
            cleanSourceEnvironment,
            message.OccurredAtUtc.ToString("O", CultureInfo.InvariantCulture),
            preparedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            idempotencyKey,
            payloadSha256);
        var signature = Convert.ToBase64String(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(signingSecret),
            Encoding.UTF8.GetBytes(signatureInput)));
        var envelope = new ControlCloudEnvelope(
            EnvelopeVersion,
            message.CloudOutboxMessageId,
            message.MessageType,
            message.SubjectType,
            message.SubjectId,
            SourceSystem,
            cleanSourceEnvironment,
            message.OccurredAtUtc,
            preparedAtUtc,
            idempotencyKey,
            payload,
            new ControlCloudEnvelopeSignature(
                SignatureAlgorithm,
                signingKeyId,
                payloadSha256,
                signature));

        return new ControlCloudReplayEnvelope(envelope, preparedAtUtc, signingKeyId, payloadSha256);
    }

    private static JsonElement CreateCanonicalPayload(
        string payloadJson,
        out string canonicalPayloadJson)
    {
        using var payloadDocument = JsonDocument.Parse(payloadJson);
        canonicalPayloadJson = JsonSerializer.Serialize(
            payloadDocument.RootElement,
            AcceptanceHttpClient.JsonOptions);
        using var canonicalPayloadDocument = JsonDocument.Parse(canonicalPayloadJson);

        return canonicalPayloadDocument.RootElement.Clone();
    }

    private static string ComputeSha256(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))
            .ToLowerInvariant();
    }
}

internal sealed record ControlCloudReplayEnvelope(
    ControlCloudEnvelope Envelope,
    DateTimeOffset PreparedAtUtc,
    string SigningKeyId,
    string PayloadSha256);
