namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.IssueLocalServerPairingDescriptor;

public sealed record IssueLocalServerPairingDescriptorCommand(
    Guid ClientId,
    string InstallationId,
    Guid? BootstrapPackageId,
    Guid? SetupTokenId,
    string? ClientCode,
    string? CustomerName,
    string? AppServerInstallationId,
    string? FingerprintHash,
    IReadOnlyCollection<string>? UrlCandidates,
    string? TlsCaSha256,
    string? TlsCertificateSha256,
    string? ServerPairingKeySha256,
    string? RequestedBy);
