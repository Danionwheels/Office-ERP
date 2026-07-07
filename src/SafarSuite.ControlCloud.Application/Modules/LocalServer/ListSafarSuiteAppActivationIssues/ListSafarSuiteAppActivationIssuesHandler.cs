using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.ListSafarSuiteAppActivationIssues;

public sealed class ListSafarSuiteAppActivationIssuesHandler
{
    private readonly IControlCloudAppActivationIssueRepository _activationIssues;

    public ListSafarSuiteAppActivationIssuesHandler(
        IControlCloudAppActivationIssueRepository activationIssues)
    {
        _activationIssues = activationIssues;
    }

    public async Task<ListSafarSuiteAppActivationIssuesResult> HandleAsync(
        ListSafarSuiteAppActivationIssuesQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.ClientId == Guid.Empty)
        {
            return ListSafarSuiteAppActivationIssuesResult.Failure(
                "ClientIdRequired",
                "Client id is required before listing app activation issues.");
        }

        if (!string.IsNullOrWhiteSpace(query.InstallationId)
            && query.InstallationId.Trim().Length > 160)
        {
            return ListSafarSuiteAppActivationIssuesResult.Failure(
                "InstallationIdInvalid",
                "Installation id cannot exceed 160 characters.");
        }

        if (!string.IsNullOrWhiteSpace(query.Query)
            && query.Query.Trim().Length > 200)
        {
            return ListSafarSuiteAppActivationIssuesResult.Failure(
                "ActivationIssueQueryInvalid",
                "Activation issue search text cannot exceed 200 characters.");
        }

        if (query.Take < 1 || query.Take > 500)
        {
            return ListSafarSuiteAppActivationIssuesResult.Failure(
                "ActivationIssueTakeInvalid",
                "Take must be between 1 and 500.");
        }

        var issues = await _activationIssues.ListAsync(
            query.ClientId,
            query.InstallationId,
            query.AppServerInstallationId,
            query.Query,
            query.Take,
            cancellationToken);

        return ListSafarSuiteAppActivationIssuesResult.Success(
            new SafarSuiteAppActivationIssuesResponse(
                issues.Select(ToResponse).ToArray()));
    }

    private static SafarSuiteAppActivationIssueResponse ToResponse(
        ControlCloudAppActivationIssue issue)
    {
        return new SafarSuiteAppActivationIssueResponse(
            issue.ActivationIssueId,
            issue.ClientId,
            issue.InstallationId,
            issue.AppServerInstallationId,
            issue.ActivationRequestId,
            issue.ReplacesActivationIssueId,
            issue.FingerprintHash,
            issue.ServerPublicKeySha256,
            issue.EntitlementVersion,
            issue.SigningKeyId,
            issue.Status,
            issue.RequestedBy,
            issue.IssuedAtUtc,
            issue.ExpiresAtUtc,
            issue.RevokedAtUtc,
            issue.RevokedBy,
            issue.RevocationReason);
    }
}
