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
    private readonly ICloudOutboxPublisherAvailability _publisherAvailability;
    private readonly ICloudOutboxPublicationLeaseProvider _publicationLeaseProvider;
    private readonly ICloudOutboxPublishPolicy _publishPolicy;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public PublishPendingCloudOutboxMessagesHandler(
        ICloudOutboxMessageRepository messages,
        ICloudOutboxPublisher publisher,
        ICloudOutboxPublisherAvailability publisherAvailability,
        ICloudOutboxPublicationLeaseProvider publicationLeaseProvider,
        ICloudOutboxPublishPolicy publishPolicy,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _messages = messages;
        _publisher = publisher;
        _publisherAvailability = publisherAvailability;
        _publicationLeaseProvider = publicationLeaseProvider;
        _publishPolicy = publishPolicy;
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

        if (!_publisherAvailability.GetSnapshot().CanPublish)
        {
            return Result<PublishPendingCloudOutboxMessagesResult>.Failure(
                ApplicationError.ServiceUnavailable(
                    "Control Cloud publisher is not securely configured.",
                    "ControlCloudPublisher"));
        }

        try
        {
            await using var publicationLease =
                await _publicationLeaseProvider.TryAcquireAsync(cancellationToken);

            if (publicationLease is null)
            {
                return Result<PublishPendingCloudOutboxMessagesResult>.Success(
                    new PublishPendingCloudOutboxMessagesResult(
                        command.BatchSize,
                        0,
                        0,
                        []));
            }

            var messages = await _messages.ListReadyForPublishingAsync(
                command.BatchSize,
                _clock.UtcNow,
                _publishPolicy.MaximumAttemptCount,
                cancellationToken);

            var publishedMessages = new List<PublishedCloudOutboxMessageResult>(messages.Count);

            foreach (var message in messages)
            {
                var publishResult = await _publisher.PublishAsync(message, cancellationToken);
                var completedAtUtc = _clock.UtcNow;

                if (publishResult.IsSuccess)
                {
                    message.MarkSent(completedAtUtc);
                }
                else
                {
                    message.MarkFailed(
                        publishResult.FailureReason!,
                        completedAtUtc,
                        ResolveNextAttemptAtUtc(message, publishResult, completedAtUtc));
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                publishedMessages.Add(ToResult(message, publishResult));
            }

            var result = new PublishPendingCloudOutboxMessagesResult(
                command.BatchSize,
                publishedMessages.Count(message => message.Status == CloudOutboxMessageStatus.Sent.ToString()),
                publishedMessages.Count(message => message.Status == CloudOutboxMessageStatus.Failed.ToString()),
                publishedMessages);

            return Result<PublishPendingCloudOutboxMessagesResult>.Success(result);
        }
        catch (ArgumentException exception)
        {
            return Result<PublishPendingCloudOutboxMessagesResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }

    private DateTimeOffset? ResolveNextAttemptAtUtc(
        CloudOutboxMessage message,
        CloudOutboxPublishResult publishResult,
        DateTimeOffset failedAtUtc)
    {
        var maximumAttemptCount = _publishPolicy.MaximumAttemptCount;

        if (!publishResult.ShouldRetry
            || (maximumAttemptCount > 0 && message.AttemptCount + 1 >= maximumAttemptCount))
        {
            return null;
        }

        return failedAtUtc.Add(_publishPolicy.RetryDelay);
    }

    private static PublishedCloudOutboxMessageResult ToResult(
        CloudOutboxMessage message,
        CloudOutboxPublishResult publishResult)
    {
        return new PublishedCloudOutboxMessageResult(
            message.Id.Value,
            message.MessageType,
            message.SubjectType,
            message.SubjectId,
            message.Status.ToString(),
            message.AttemptCount,
            message.LastAttemptedAtUtc,
            message.NextAttemptAtUtc,
            message.SentAtUtc,
            message.FailedAtUtc,
            message.FailureReason,
            publishResult.CloudReference,
            publishResult.EnvelopeSignature);
    }
}
