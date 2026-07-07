namespace SafarSuite.LocalServer.Application.Commands.GetAppActivationRevocationStatus;

public sealed record GetAppActivationRevocationStatusQuery(
    Guid ClientId,
    string InstallationId,
    Guid AppServerInstallationId,
    Guid ActivationIssueId,
    string? FingerprintHash,
    string? ServerPublicKeySha256,
    string? RequestedBy);
