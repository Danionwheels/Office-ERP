namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.MarkCloudInstallationBootstrapPackageHandoff;

public sealed record MarkCloudInstallationBootstrapPackageHandoffCommand(
    Guid ClientId,
    string InstallationId,
    Guid BootstrapPackageId,
    string Channel,
    string Recipient,
    string MarkedBy,
    string? Note);
