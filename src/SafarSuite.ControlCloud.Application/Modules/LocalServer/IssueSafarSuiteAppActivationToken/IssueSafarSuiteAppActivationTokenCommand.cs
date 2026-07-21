namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.IssueSafarSuiteAppActivationToken;

public sealed record IssueSafarSuiteAppActivationTokenCommand(
    Guid ClientId,
    string InstallationId,
    Guid? ActivationRequestId,
    Guid ServerInstallationId,
    string FingerprintHash,
    string ServerPublicKey,
    string? RequestedBy,
    Guid? ReplacesActivationIssueId);
