using SafarSuite.ControlDesk.Api.Common;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListCloudOutboxMessages;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.ControlCloud;

namespace SafarSuite.ControlDesk.Api.Modules.ControlCloud;

public static class ControlCloudEndpoints
{
    public static IEndpointRouteBuilder MapControlCloudEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/control-cloud")
            .WithTags("Control Cloud");

        group.MapGet("/outbox-messages", ListOutboxMessagesAsync);

        return endpoints;
    }

    private static async Task<IResult> ListOutboxMessagesAsync(
        string? status,
        string? messageType,
        ListCloudOutboxMessagesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListCloudOutboxMessagesQuery(status, messageType),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new ListCloudOutboxMessagesResponse(
            result.Value.Messages.Select(message => new CloudOutboxMessageResponse(
                message.CloudOutboxMessageId,
                message.MessageType,
                message.SubjectType,
                message.SubjectId,
                message.PayloadJson,
                message.Status,
                message.AttemptCount,
                message.OccurredAtUtc,
                message.SentAtUtc,
                message.FailedAtUtc,
                message.FailureReason)).ToArray());

        return Results.Ok(response);
    }
}
