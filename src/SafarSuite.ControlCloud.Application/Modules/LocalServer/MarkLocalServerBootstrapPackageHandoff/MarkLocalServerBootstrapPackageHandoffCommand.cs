namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.MarkLocalServerBootstrapPackageHandoff;

public sealed record MarkLocalServerBootstrapPackageHandoffCommand(
    Guid ClientId,
    string InstallationId,
    Guid BootstrapPackageId,
    string Channel,
    string Recipient,
    string MarkedBy,
    IReadOnlyCollection<string> PreflightAcknowledgements,
    string? Note);
