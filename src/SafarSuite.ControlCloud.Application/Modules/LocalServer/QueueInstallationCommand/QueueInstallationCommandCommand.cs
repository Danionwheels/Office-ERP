namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.QueueInstallationCommand;

public sealed record QueueInstallationCommandCommand(
    Guid ClientId,
    string InstallationId,
    string CommandType,
    string PayloadJson,
    DateTimeOffset? NotBeforeUtc,
    DateTimeOffset? ExpiresAtUtc,
    string? IdempotencyKey);
