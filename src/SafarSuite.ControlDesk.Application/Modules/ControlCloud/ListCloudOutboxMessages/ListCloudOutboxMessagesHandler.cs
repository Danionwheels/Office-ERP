using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListCloudOutboxMessages;

public sealed class ListCloudOutboxMessagesHandler
{
    private readonly ICloudOutboxMessageRepository _messages;

    public ListCloudOutboxMessagesHandler(ICloudOutboxMessageRepository messages)
    {
        _messages = messages;
    }

    public async Task<Result<ListCloudOutboxMessagesResult>> HandleAsync(
        ListCloudOutboxMessagesQuery query,
        CancellationToken cancellationToken = default)
    {
        CloudOutboxMessageStatus? status = null;

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<CloudOutboxMessageStatus>(query.Status, ignoreCase: true, out var parsedStatus)
                || !Enum.IsDefined(parsedStatus))
            {
                return Result<ListCloudOutboxMessagesResult>.Failure(ApplicationError.Validation(
                    nameof(query.Status),
                    "Cloud outbox status is not valid."));
            }

            status = parsedStatus;
        }

        var messages = await _messages.ListAsync(status, query.MessageType, cancellationToken);

        return Result<ListCloudOutboxMessagesResult>.Success(new ListCloudOutboxMessagesResult(
            messages.Select(ToSummary).ToArray()));
    }

    private static CloudOutboxMessageSummaryResult ToSummary(CloudOutboxMessage message)
    {
        return new CloudOutboxMessageSummaryResult(
            message.Id.Value,
            message.MessageType,
            message.SubjectType,
            message.SubjectId,
            message.PayloadJson,
            message.Status.ToString(),
            message.AttemptCount,
            message.OccurredAtUtc,
            message.SentAtUtc,
            message.FailedAtUtc,
            message.FailureReason);
    }
}
