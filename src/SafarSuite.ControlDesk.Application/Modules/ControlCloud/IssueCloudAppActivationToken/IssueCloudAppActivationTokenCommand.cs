namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.IssueCloudAppActivationToken;

public sealed record IssueCloudAppActivationTokenCommand(
    Guid ClientId,
    string InstallationId,
    Guid? ActivationRequestId,
    Guid ServerInstallationId,
    string FingerprintHash,
    string ServerPublicKey,
    string RequestedBy,
    Guid? ReplacesActivationIssueId);
