namespace SafarSuite.ControlCloud.Domain.Modules.LocalServer;

public sealed record ControlCloudInstallationCommandAcknowledgement(
    Guid AcknowledgementId,
    Guid CommandId,
    Guid ClientId,
    string InstallationId,
    long CommandVersion,
    string ResultStatus,
    string? Detail,
    string PayloadJson,
    DateTimeOffset AcknowledgedAtUtc);
