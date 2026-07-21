using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListCloudAppActivationIssues;

public sealed record ListCloudAppActivationIssuesResult(
    IReadOnlyCollection<SafarSuiteAppActivationIssueResponse> Issues);
