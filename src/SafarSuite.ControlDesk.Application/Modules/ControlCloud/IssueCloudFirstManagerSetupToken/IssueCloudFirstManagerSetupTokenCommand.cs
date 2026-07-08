namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.IssueCloudFirstManagerSetupToken;

public sealed record IssueCloudFirstManagerSetupTokenCommand(
    Guid ClientId,
    string InstallationId,
    Guid PendingDeviceRequestId,
    string ManagerDisplayName,
    string? ManagerEmail,
    string? CreatedBy,
    int ExpiresInHours);
