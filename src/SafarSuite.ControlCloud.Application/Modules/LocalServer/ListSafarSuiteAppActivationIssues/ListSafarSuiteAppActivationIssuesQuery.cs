namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.ListSafarSuiteAppActivationIssues;

public sealed record ListSafarSuiteAppActivationIssuesQuery(
    Guid ClientId,
    string? InstallationId,
    Guid? AppServerInstallationId,
    string? Query,
    int Take);
