using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.PublishPendingCloudOutboxMessages;

public sealed class PublishPendingCloudOutboxMessagesHandler
{
    private const int MaximumBatchSize = 100;

    private readonly ICloudOutboxMessageRepository _messages;
    private readonly ICloudOutboxPublisher _publisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public PublishPendingCloudOutboxMessagesHandler(
        ICloudOutboxMessageRepository messages,
        ICloudOutboxPublisher publisher,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _messages = messages;
        _publisher = publisher;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<PublishPendingCloudOutboxMessagesResult>> HandleAsync(
        PublishPendingCloudOutboxMessagesCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.BatchSize is < 1 or > MaximumBatchSize)
        {
            return Result<PublishPendingCloudOutboxMessagesResult>.Failure(ApplicationError.Validation(
                nameof(command.BatchSize),
                $"Batch size must be between 1 and {MaximumBatchSize}."));
        }

        try
        {
            var result = await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var messages = await _messages.ListPendingForPublishingAsync(command.BatchSize, token);
                    var publishedMessages = new List<PublishedCloudOutboxMessageResult>(messages.Count);

                    foreach (var message in messages)
                    {
                        var publishResult = await _publisher.PublishAsync(message, token);

                        if (publishResult.IsSuccess)
                        {
                            message.MarkSent(_clock.UtcNow);
                        }
                        else
                        {
                            message.MarkFailed(publishResult.FailureReason!, _clock.UtcNow);
                        }

                        publishedMessages.Add(ToResult(message));
                    }

                    return new PublishPendingCloudOutboxMessagesResult(
                        command.BatchSize,
                        publishedMessages.Count(message => message.Status == CloudOutboxMessageStatus.Sent.ToString()),
                        publishedMessages.Count(message => message.Status == CloudOutboxMessageStatus.Failed.ToString()),
                        publishedMessages);
                },
                cancellationToken);

            return Result<PublishPendingCloudOutboxMessagesResult>.Success(result);
        }
        catch (ArgumentException exception)
        {
            return Result<PublishPendingCloudOutboxMessagesResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }

    private static PublishedCloudOutboxMessageResult ToResult(CloudOutboxMessage message)
    {
        return new PublishedCloudOutboxMessageResult(
            message.Id.Value,
            message.MessageType,
            message.SubjectType,
            message.SubjectId,
            message.Status.ToString(),
            message.AttemptCount,
            message.SentAtUtc,
            message.FailedAtUtc,
            message.FailureReason);
    }
}
