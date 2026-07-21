using System.Globalization;
using System.Text;
using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListCloudOutboxMessages;

public sealed class ListCloudOutboxMessagesHandler
{
    private const int MaximumPageSize = 100;

    private readonly ICloudOutboxMessageRepository _messages;
    private readonly ICloudOutboxPublishPolicy _publishPolicy;
    private readonly IClock _clock;

    public ListCloudOutboxMessagesHandler(
        ICloudOutboxMessageRepository messages,
        ICloudOutboxPublishPolicy publishPolicy,
        IClock clock)
    {
        _messages = messages;
        _publishPolicy = publishPolicy;
        _clock = clock;
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

        if (query.ClientId == Guid.Empty)
        {
            return Result<ListCloudOutboxMessagesResult>.Failure(ApplicationError.Validation(
                nameof(query.ClientId),
                "Client id must not be empty."));
        }

        if (query.Take is < 1 or > MaximumPageSize)
        {
            return Result<ListCloudOutboxMessagesResult>.Failure(ApplicationError.Validation(
                nameof(query.Take),
                $"Page size must be between 1 and {MaximumPageSize}."));
        }

        if (!TryDecodeCursor(query.Cursor, out var beforeOccurredAtUtc, out var beforeMessageId))
        {
            return Result<ListCloudOutboxMessagesResult>.Failure(ApplicationError.Validation(
                nameof(query.Cursor),
                "Cloud outbox cursor is invalid or malformed."));
        }

        var clientId = query.ClientId.HasValue
            ? ClientId.Create(query.ClientId.Value)
            : (ClientId?)null;
        var messages = await _messages.ListPageAsync(
            status,
            query.MessageType,
            clientId,
            beforeOccurredAtUtc,
            beforeMessageId,
            query.Take + 1,
            cancellationToken);
        var hasMore = messages.Count > query.Take;
        var items = messages.Take(query.Take).Select(ToSummary).ToArray();
        var nextCursor = hasMore && items.Length > 0
            ? EncodeCursor(items[^1])
            : null;
        var summary = await _messages.SummarizeAsync(
            status,
            query.MessageType,
            clientId,
            _clock.UtcNow,
            _publishPolicy.MaximumAttemptCount,
            cancellationToken);

        return Result<ListCloudOutboxMessagesResult>.Success(new ListCloudOutboxMessagesResult(
            items,
            query.Take,
            hasMore,
            nextCursor,
            new CloudOutboxMessageRegisterSummaryResult(
                summary.TotalCount,
                summary.PendingCount,
                summary.FailedCount,
                summary.SentCount,
                summary.ReadyForPublishingCount,
                summary.TotalAttemptCount)));
    }

    private static CloudOutboxMessageSummaryResult ToSummary(CloudOutboxMessage message)
    {
        return new CloudOutboxMessageSummaryResult(
            message.Id.Value,
            message.ClientId?.Value,
            message.MessageType,
            message.SubjectType,
            message.SubjectId,
            message.PayloadJson,
            message.Status.ToString(),
            message.AttemptCount,
            message.OccurredAtUtc,
            message.LastAttemptedAtUtc,
            message.NextAttemptAtUtc,
            message.SentAtUtc,
            message.FailedAtUtc,
            message.FailureReason);
    }

    private static string EncodeCursor(CloudOutboxMessageSummaryResult message)
    {
        var value = string.Create(
            CultureInfo.InvariantCulture,
            $"{message.OccurredAtUtc.ToUniversalTime().Ticks}|{message.CloudOutboxMessageId:N}");

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static bool TryDecodeCursor(
        string? cursor,
        out DateTimeOffset? beforeOccurredAtUtc,
        out CloudOutboxMessageId? beforeMessageId)
    {
        beforeOccurredAtUtc = null;
        beforeMessageId = null;

        if (string.IsNullOrWhiteSpace(cursor))
        {
            return true;
        }

        try
        {
            var normalized = cursor.Trim().Replace('-', '+').Replace('_', '/');
            normalized = normalized.PadRight(normalized.Length + ((4 - normalized.Length % 4) % 4), '=');
            var value = Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
            var parts = value.Split('|', StringSplitOptions.TrimEntries);

            if (parts.Length != 2
                || !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
                || !Guid.TryParseExact(parts[1], "N", out var messageId))
            {
                return false;
            }

            beforeOccurredAtUtc = new DateTimeOffset(ticks, TimeSpan.Zero);
            beforeMessageId = CloudOutboxMessageId.Create(messageId);

            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}
