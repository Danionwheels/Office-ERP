using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;
using SafarSuite.ControlCloud.Infrastructure.ClientPortal;

namespace SafarSuite.ControlCloud.Infrastructure.LocalServer;

public sealed class HmacControlCloudInstallationCommandSigner
    : IControlCloudInstallationCommandSigner
{
    private const string SignatureAlgorithm = "HMAC-SHA256";
    private const string CommandSignatureVersion = "1";
    private const long TicksPerMicrosecond = 10;

    private readonly string _activeKeyId;
    private readonly IReadOnlyDictionary<string, string> _secretsByKeyId;

    public HmacControlCloudInstallationCommandSigner(
        ControlCloudEntitlementSigningOptions options)
    {
        _activeKeyId = options.ActiveKeyId.Trim();
        _secretsByKeyId = options.SigningKeys
            .Where(key => !string.IsNullOrWhiteSpace(key.KeyId) && !string.IsNullOrWhiteSpace(key.Secret))
            .GroupBy(key => key.KeyId.Trim(), StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.First().Secret.Trim(),
                StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(_activeKeyId))
        {
            throw new InvalidOperationException(
                "ControlCloud:EntitlementSigning:ActiveKeyId is required for command signing.");
        }

        if (!_secretsByKeyId.ContainsKey(_activeKeyId))
        {
            throw new InvalidOperationException(
                $"ControlCloud command signing key '{_activeKeyId}' is not configured.");
        }
    }

    public ControlCloudInstallationCommandSignature Sign(
        ControlCloudInstallationCommandSigningPayload payload)
    {
        var canonicalPayloadJson = CanonicalizeJson(payload.PayloadJson);
        var payloadSha256 = ComputeSha256(canonicalPayloadJson);
        var signatureValue = Sign(
            _secretsByKeyId[_activeKeyId],
            BuildSignatureInput(payload, payloadSha256));

        return new ControlCloudInstallationCommandSignature(
            SignatureAlgorithm,
            _activeKeyId,
            payloadSha256,
            signatureValue);
    }

    private static string BuildSignatureInput(
        ControlCloudInstallationCommandSigningPayload payload,
        string payloadSha256)
    {
        var queuedAtUtc = NormalizeTimestamp(payload.QueuedAtUtc);
        DateTimeOffset? notBeforeUtc = payload.NotBeforeUtc is null
            ? null
            : NormalizeTimestamp(payload.NotBeforeUtc.Value);
        var expiresAtUtc = NormalizeTimestamp(payload.ExpiresAtUtc);

        return string.Join(
            "\n",
            CommandSignatureVersion,
            payload.CommandId.ToString("D", CultureInfo.InvariantCulture),
            payload.ClientId.ToString("D", CultureInfo.InvariantCulture),
            payload.InstallationId,
            payload.CommandVersion.ToString(CultureInfo.InvariantCulture),
            payload.CommandType,
            queuedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            notBeforeUtc?.ToString("O", CultureInfo.InvariantCulture) ?? "",
            expiresAtUtc.ToString("O", CultureInfo.InvariantCulture),
            payloadSha256);
    }

    private static DateTimeOffset NormalizeTimestamp(DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        var ticks = utc.Ticks - (utc.Ticks % TicksPerMicrosecond);

        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }

    private static string CanonicalizeJson(string value)
    {
        using var document = JsonDocument.Parse(
            string.IsNullOrWhiteSpace(value) ? "{}" : value);
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        WriteCanonicalJson(writer, document.RootElement);
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteCanonicalJson(
        Utf8JsonWriter writer,
        JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject()
                             .OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonicalJson(writer, property.Value);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteCanonicalJson(writer, item);
                }
                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
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
}
