using SafarSuite.ControlCloud.Application.Modules.Audit.ListControlCloudAuditEvents;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Api.Modules.Audit;

public static class ControlCloudAuditEndpoints
{
    public static IEndpointRouteBuilder MapControlCloudAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/control-cloud/audit-events")
            .WithTags("Control Cloud Audit");

        group.MapGet("", ListAuditEventsAsync)
            .WithName("ListControlCloudAuditEvents");

        return endpoints;
    }

    private static async Task<IResult> ListAuditEventsAsync(
        Guid? clientId,
        string? eventType,
        int? take,
        ListControlCloudAuditEventsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListControlCloudAuditEventsQuery(
                clientId,
                eventType,
                take ?? 100),
            cancellationToken);

        return Results.Ok(new ControlCloudAuditEventsResponse(
            result.Events.Select(ToResponse).ToArray()));
    }

    private static ControlCloudAuditEventResponse ToResponse(
        ClientPortalAuditRecord record)
    {
        return new ControlCloudAuditEventResponse(
            record.AuditEventId,
            record.ClientId,
            record.InvitationId,
            record.UserId,
            record.SubjectEmail,
            record.EventType,
            record.Actor,
            record.Detail,
            record.OccurredAtUtc);
    }
}
