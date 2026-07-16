using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Paging;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.ListClients;

public sealed class ListClientsHandler
{
    private const int MaximumPageSize = 100;
    private const int MaximumSearchLength = 128;

    private readonly IClientDirectoryReader _clients;

    public ListClientsHandler(IClientDirectoryReader clients)
    {
        _clients = clients;
    }

    public async Task<Result<ListClientsResult>> HandleAsync(
        ListClientsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Take is < 1 or > MaximumPageSize)
        {
            return Result<ListClientsResult>.Failure(ApplicationError.Validation(
                nameof(query.Take),
                $"Page size must be between 1 and {MaximumPageSize}."));
        }

        var search = query.Search?.Trim() ?? string.Empty;

        if (search.Length > MaximumSearchLength)
        {
            return Result<ListClientsResult>.Failure(ApplicationError.Validation(
                nameof(query.Search),
                $"Search text must be {MaximumSearchLength} characters or fewer."));
        }

        string? status = null;

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<ClientStatus>(query.Status, ignoreCase: true, out var parsedStatus)
                || !Enum.IsDefined(parsedStatus))
            {
                return Result<ListClientsResult>.Failure(ApplicationError.Validation(
                    nameof(query.Status),
                    "Client status is not valid."));
            }

            status = parsedStatus.ToString();
        }

        if (!TryParseSort(query.Sort, out var sort))
        {
            return Result<ListClientsResult>.Failure(ApplicationError.Validation(
                nameof(query.Sort),
                "Client sort must be code, displayName, legalName, or status."));
        }

        if (!TryParseDirection(query.Direction, out var direction))
        {
            return Result<ListClientsResult>.Failure(ApplicationError.Validation(
                nameof(query.Direction),
                "Client sort direction must be asc or desc."));
        }

        if (!OpaqueCursor.TryDecode<ClientDirectoryCursor>(query.Cursor, out var cursor)
            || !CursorMatches(cursor, search, status, sort, direction))
        {
            return Result<ListClientsResult>.Failure(ApplicationError.Validation(
                nameof(query.Cursor),
                "Client directory cursor is invalid, malformed, or belongs to another query."));
        }

        var page = await _clients.ReadPageAsync(
            new ClientDirectoryReadRequest(
                search,
                status,
                sort,
                direction,
                cursor?.SortValue,
                cursor?.Code,
                cursor?.ClientId,
                query.Take + 1),
            cancellationToken);
        var hasMore = page.Items.Count > query.Take;
        var items = page.Items.Take(query.Take).ToArray();
        var nextCursor = hasMore && items.Length > 0
            ? OpaqueCursor.Encode(new ClientDirectoryCursor(
                1,
                NormalizeSearch(search),
                status,
                sort.ToString(),
                direction.ToString(),
                items[^1].SortValue,
                items[^1].Code,
                items[^1].ClientId))
            : null;

        return Result<ListClientsResult>.Success(new ListClientsResult(
            items.Select(client => new ClientLookupResult(
                client.ClientId,
                client.Code,
                client.LegalName,
                client.DisplayName,
                client.Status)).ToArray(),
            query.Take,
            hasMore,
            nextCursor,
            page.FilteredCount,
            new ClientDirectorySummaryResult(
                page.Summary.TotalCount,
                page.Summary.DraftCount,
                page.Summary.ActiveCount,
                page.Summary.SuspendedCount,
                page.Summary.ArchivedCount)));
    }

    private static bool TryParseSort(string? value, out ClientDirectorySort sort)
    {
        sort = ClientDirectorySort.Code;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "code" => SetSort(ClientDirectorySort.Code, out sort),
            "displayname" => SetSort(ClientDirectorySort.DisplayName, out sort),
            "legalname" => SetSort(ClientDirectorySort.LegalName, out sort),
            "status" => SetSort(ClientDirectorySort.Status, out sort),
            _ => false
        };
    }

    private static bool TryParseDirection(
        string? value,
        out ClientDirectorySortDirection direction)
    {
        direction = ClientDirectorySortDirection.Ascending;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "asc" => true,
            "desc" => SetDirection(ClientDirectorySortDirection.Descending, out direction),
            _ => false
        };
    }

    private static bool CursorMatches(
        ClientDirectoryCursor? cursor,
        string search,
        string? status,
        ClientDirectorySort sort,
        ClientDirectorySortDirection direction)
    {
        if (cursor is null)
        {
            return true;
        }

        return cursor.Version == 1
            && cursor.ClientId != Guid.Empty
            && !string.IsNullOrWhiteSpace(cursor.SortValue)
            && !string.IsNullOrWhiteSpace(cursor.Code)
            && string.Equals(cursor.Search, NormalizeSearch(search), StringComparison.Ordinal)
            && string.Equals(cursor.Status, status, StringComparison.Ordinal)
            && string.Equals(cursor.Sort, sort.ToString(), StringComparison.Ordinal)
            && string.Equals(cursor.Direction, direction.ToString(), StringComparison.Ordinal);
    }

    private static string NormalizeSearch(string search) => search.ToLowerInvariant();

    private static bool SetSort(ClientDirectorySort value, out ClientDirectorySort sort)
    {
        sort = value;
        return true;
    }

    private static bool SetDirection(
        ClientDirectorySortDirection value,
        out ClientDirectorySortDirection direction)
    {
        direction = value;
        return true;
    }

    private sealed record ClientDirectoryCursor(
        int Version,
        string Search,
        string? Status,
        string Sort,
        string Direction,
        string SortValue,
        string Code,
        Guid ClientId);
}
