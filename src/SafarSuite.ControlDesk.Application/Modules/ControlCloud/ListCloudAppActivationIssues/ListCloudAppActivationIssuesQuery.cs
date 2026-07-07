namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListCloudAppActivationIssues;

public sealed record ListCloudAppActivationIssuesQuery(
    Guid ClientId,
    string? InstallationId,
    Guid? AppServerInstallationId,
    string? Query,
    int Take);
