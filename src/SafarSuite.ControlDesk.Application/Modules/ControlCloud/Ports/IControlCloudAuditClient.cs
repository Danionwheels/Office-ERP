using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;

public interface IControlCloudAuditClient
{
    Task<ControlCloudAuditClientResult> ListEventsAsync(
        Guid clientId,
        int take,
        CancellationToken cancellationToken = default);
}

public sealed class ControlCloudAuditClientResult
{
    private ControlCloudAuditClientResult(
        IReadOnlyList<ControlCloudAuditEventResponse>? events,
        string? failureCode,
        string? detail)
    {
        Events = events;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Events is not null;

    public IReadOnlyList<ControlCloudAuditEventResponse>? Events { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ControlCloudAuditClientResult Success(
        IReadOnlyList<ControlCloudAuditEventResponse> events)
    {
        return new ControlCloudAuditClientResult(
            events,
            failureCode: null,
            detail: null);
    }

    public static ControlCloudAuditClientResult Failure(
        string failureCode,
        string detail)
    {
        return new ControlCloudAuditClientResult(
            events: null,
            failureCode,
            detail);
    }
}
