namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.RevokeCloudAppActivationIssue;

public sealed record RevokeCloudAppActivationIssueCommand(
    Guid ClientId,
    Guid ActivationIssueId,
    string RevokedBy,
    string Reason);
