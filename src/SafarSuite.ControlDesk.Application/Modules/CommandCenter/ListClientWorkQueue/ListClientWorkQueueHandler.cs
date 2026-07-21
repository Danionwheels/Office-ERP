using SafarSuite.ControlDesk.Application.Common.Paging;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.CommandCenter.Ports;

namespace SafarSuite.ControlDesk.Application.Modules.CommandCenter.ListClientWorkQueue;

public sealed class ListClientWorkQueueHandler
{
    private const int MaximumPageSize = 100;
    private const int MaximumSearchLength = 128;

    private readonly IClientWorkQueueReader _queue;

    public ListClientWorkQueueHandler(IClientWorkQueueReader queue)
    {
        _queue = queue;
    }

    public async Task<Result<ListClientWorkQueueResult>> HandleAsync(
        ListClientWorkQueueQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Take is < 1 or > MaximumPageSize)
        {
            return Result<ListClientWorkQueueResult>.Failure(ApplicationError.Validation(
                nameof(query.Take),
                $"Page size must be between 1 and {MaximumPageSize}."));
        }

        var search = query.Search?.Trim() ?? string.Empty;

        if (search.Length > MaximumSearchLength)
        {
            return Result<ListClientWorkQueueResult>.Failure(ApplicationError.Validation(
                nameof(query.Search),
                $"Search text must be {MaximumSearchLength} characters or fewer."));
        }

        if (!TryParseLane(query.Lane, out var lane))
        {
            return Result<ListClientWorkQueueResult>.Failure(ApplicationError.Validation(
                nameof(query.Lane),
                "Queue lane must be all, setup, billing, payments, access, cloud, or overview."));
        }

        if (!TryParseSort(query.Sort, out var sort))
        {
            return Result<ListClientWorkQueueResult>.Failure(ApplicationError.Validation(
                nameof(query.Sort),
                "Queue sort must be priority, client, or action."));
        }

        if (!OpaqueCursor.TryDecode<ClientWorkQueueCursor>(query.Cursor, out var cursor)
            || !CursorMatches(cursor, search, lane, sort))
        {
            return Result<ListClientWorkQueueResult>.Failure(ApplicationError.Validation(
                nameof(query.Cursor),
                "Client work cursor is invalid, malformed, or belongs to another query."));
        }

        var page = await _queue.ReadPageAsync(
            new ClientWorkQueueReadRequest(
                search,
                lane,
                sort,
                cursor?.SortValue,
                cursor?.Priority,
                cursor?.Code,
                cursor?.ClientId,
                query.Take + 1),
            cancellationToken);
        var hasMore = page.Items.Count > query.Take;
        var items = page.Items.Take(query.Take).ToArray();
        var nextCursor = hasMore && items.Length > 0
            ? OpaqueCursor.Encode(new ClientWorkQueueCursor(
                1,
                search.ToLowerInvariant(),
                lane.ToString(),
                sort.ToString(),
                items[^1].SortValue,
                items[^1].Priority,
                items[^1].Code,
                items[^1].ClientId))
            : null;

        return Result<ListClientWorkQueueResult>.Success(new ListClientWorkQueueResult(
            items.Select(item => new ClientWorkQueueItemResult(
                item.ClientId,
                item.Code,
                item.Name,
                item.Status,
                item.ActionLabel,
                item.Detail,
                item.Tab,
                item.Tone,
                item.Priority)).ToArray(),
            query.Take,
            hasMore,
            nextCursor,
            page.FilteredCount,
            new ClientWorkQueueSummaryResult(
                page.Summary.TotalCount,
                page.Summary.SetupCount,
                page.Summary.BillingCount,
                page.Summary.PaymentsCount,
                page.Summary.AccessCount,
                page.Summary.CloudCount,
                page.Summary.OverviewCount)));
    }

    private static bool TryParseLane(string? value, out ClientWorkQueueLane lane)
    {
        lane = ClientWorkQueueLane.All;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return Enum.TryParse(value.Trim(), ignoreCase: true, out lane) && Enum.IsDefined(lane);
    }

    private static bool TryParseSort(string? value, out ClientWorkQueueSort sort)
    {
        sort = ClientWorkQueueSort.Priority;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return Enum.TryParse(value.Trim(), ignoreCase: true, out sort) && Enum.IsDefined(sort);
    }

    private static bool CursorMatches(
        ClientWorkQueueCursor? cursor,
        string search,
        ClientWorkQueueLane lane,
        ClientWorkQueueSort sort)
    {
        if (cursor is null)
        {
            return true;
        }

        return cursor.Version == 1
            && cursor.ClientId != Guid.Empty
            && cursor.Priority is >= 0 and <= 7
            && !string.IsNullOrWhiteSpace(cursor.SortValue)
            && !string.IsNullOrWhiteSpace(cursor.Code)
            && string.Equals(cursor.Search, search.ToLowerInvariant(), StringComparison.Ordinal)
            && string.Equals(cursor.Lane, lane.ToString(), StringComparison.Ordinal)
            && string.Equals(cursor.Sort, sort.ToString(), StringComparison.Ordinal);
    }

    private sealed record ClientWorkQueueCursor(
        int Version,
        string Search,
        string Lane,
        string Sort,
        string SortValue,
        int Priority,
        string Code,
        Guid ClientId);
}
