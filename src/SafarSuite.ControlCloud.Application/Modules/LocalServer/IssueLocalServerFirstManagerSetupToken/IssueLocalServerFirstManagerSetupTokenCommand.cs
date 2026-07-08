namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.IssueLocalServerFirstManagerSetupToken;

public sealed record IssueLocalServerFirstManagerSetupTokenCommand(
    Guid ClientId,
    string InstallationId,
    Guid PendingDeviceRequestId,
    string ManagerDisplayName,
    string? ManagerEmail,
    string? CreatedBy,
    int ExpiresInHours);
