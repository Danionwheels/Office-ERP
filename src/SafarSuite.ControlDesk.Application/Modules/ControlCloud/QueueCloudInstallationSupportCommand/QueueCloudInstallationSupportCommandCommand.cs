namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.QueueCloudInstallationSupportCommand;

public sealed record QueueCloudInstallationSupportCommandCommand(
    Guid ClientId,
    string InstallationId,
    string CommandType,
    string Reason,
    string RequestedBy,
    int ExpiresInHours);
