using SafarSuite.ControlCloud.Application.Modules.Audit.Ports;

namespace SafarSuite.ControlCloud.Application.Modules.Audit.ListControlCloudAuditEvents;

public sealed class ListControlCloudAuditEventsHandler
{
    private readonly IControlCloudAuditEventReader _reader;

    public ListControlCloudAuditEventsHandler(IControlCloudAuditEventReader reader)
    {
        _reader = reader;
    }

    public async Task<ListControlCloudAuditEventsResult> HandleAsync(
        ListControlCloudAuditEventsQuery query,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(query.Take, 1, 500);
        var eventType = string.IsNullOrWhiteSpace(query.EventType)
            ? null
            : query.EventType.Trim();
        var events = await _reader.ListAsync(
            query.ClientId,
            eventType,
            take,
            cancellationToken);

        return new ListControlCloudAuditEventsResult(events);
    }
}
