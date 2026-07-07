namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.RevokeSafarSuiteAppActivationIssue;

public sealed record RevokeSafarSuiteAppActivationIssueCommand(
    Guid ClientId,
    Guid ActivationIssueId,
    string RevokedBy,
    string Reason);
