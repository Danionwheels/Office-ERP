using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListCloudInstallationAuditEvents;

public sealed class ListCloudInstallationAuditEventsHandler
{
    private static readonly HashSet<string> InstallationEventTypes = new(StringComparer.Ordinal)
    {
        "SetupTokenCreated",
        "BootstrapPackageGenerated",
        "LocalServerRegistrationAccepted",
        "LocalServerRegistrationRejected",
        "LocalServerDiagnosticsUploaded",
        "OfflineRenewalFileGenerated"
    };

    private readonly IControlCloudAuditClient _auditClient;

    public ListCloudInstallationAuditEventsHandler(
        IControlCloudAuditClient auditClient)
    {
        _auditClient = auditClient;
    }

    public async Task<Result<ControlCloudAuditEventsResponse>> HandleAsync(
        ListCloudInstallationAuditEventsQuery query,
        CancellationToken cancellationToken = default)
    {
        var installationId = query.InstallationId.Trim();

        if (query.ClientId == Guid.Empty)
        {
            return Result<ControlCloudAuditEventsResponse>.Failure(
                ApplicationError.Validation(nameof(query.ClientId), "Client id is required."));
        }

        if (string.IsNullOrWhiteSpace(installationId))
        {
            return Result<ControlCloudAuditEventsResponse>.Failure(
                ApplicationError.Validation(nameof(query.InstallationId), "Installation id is required."));
        }

        var result = await _auditClient.ListEventsAsync(
            query.ClientId,
            Math.Clamp(query.Take, 1, 200),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Result<ControlCloudAuditEventsResponse>.Failure(
                ToApplicationError(result));
        }

        var events = result.Events!
            .Where(IsInstallationEvent)
            .Where(auditEvent => ContainsInstallationId(auditEvent, installationId))
            .OrderByDescending(auditEvent => auditEvent.OccurredAtUtc)
            .Take(Math.Clamp(query.Take, 1, 100))
            .ToArray();

        return Result<ControlCloudAuditEventsResponse>.Success(
            new ControlCloudAuditEventsResponse(events));
    }

    private static bool IsInstallationEvent(ControlCloudAuditEventResponse auditEvent)
    {
        return InstallationEventTypes.Contains(auditEvent.EventType);
    }

    private static bool ContainsInstallationId(
        ControlCloudAuditEventResponse auditEvent,
        string installationId)
    {
        return auditEvent.Detail.Contains(
            installationId,
            StringComparison.OrdinalIgnoreCase);
    }

    private static ApplicationError ToApplicationError(
        ControlCloudAuditClientResult result)
    {
        return result.FailureCode switch
        {
            "ControlCloudAuditNotConfigured" => ApplicationError.Unexpected(
                result.Detail ?? "Control Cloud audit endpoint is not configured."),
            "ControlCloudAuditUnavailable" => ApplicationError.Unexpected(
                result.Detail ?? "Control Cloud audit is unavailable."),
            "ControlCloudAuditResponseInvalid" => ApplicationError.Unexpected(
                result.Detail ?? "Control Cloud returned an invalid audit response."),
            _ => ApplicationError.Unexpected(
                result.Detail ?? "Control Cloud audit events could not be loaded.")
        };
    }
}
