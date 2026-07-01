using System.Text.Json;

namespace SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

public sealed record ControlCloudEnvelope(
    string EnvelopeVersion,
    Guid MessageId,
    string MessageType,
    string SubjectType,
    string SubjectId,
    string SourceSystem,
    string SourceEnvironment,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset PreparedAtUtc,
    string IdempotencyKey,
    JsonElement Payload,
    ControlCloudEnvelopeSignature Signature);

public sealed record ControlCloudEnvelopeSignature(
    string Algorithm,
    string KeyId,
    string PayloadSha256,
    string Value);
