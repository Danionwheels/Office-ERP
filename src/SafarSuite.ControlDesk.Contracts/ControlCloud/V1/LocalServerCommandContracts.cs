using System.Text.Json;

namespace SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

public sealed record QueueInstallationCommandRequest(
    string CommandType,
    JsonElement Payload,
    DateTimeOffset? NotBeforeUtc,
    DateTimeOffset? ExpiresAtUtc,
    string? IdempotencyKey);

public sealed record AcknowledgeInstallationCommandRequest(
    string ResultStatus,
    string? Detail,
    JsonElement Payload);

public sealed record InstallationCommandResponse(
    Guid CommandId,
    Guid ClientId,
    string InstallationId,
    long CommandVersion,
    string CommandType,
    string Status,
    string IdempotencyKey,
    JsonElement Payload,
    InstallationCommandSignatureResponse Signature,
    DateTimeOffset QueuedAtUtc,
    DateTimeOffset? NotBeforeUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? AcknowledgedAtUtc,
    string? AcknowledgementStatus,
    string? AcknowledgementDetail);

public sealed record PendingInstallationCommandsResponse(
    string InstallationId,
    IReadOnlyCollection<InstallationCommandResponse> Commands);

public sealed record InstallationCommandSignatureResponse(
    string Algorithm,
    string KeyId,
    string PayloadSha256,
    string Value);
