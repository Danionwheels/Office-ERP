namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.ExportOfflineRenewalFile;

public sealed record ExportOfflineRenewalFileQuery(
    Guid ClientId,
    string InstallationId,
    string GeneratedBy,
    string Reason);
