namespace SafarSuite.ControlDesk.Application.Modules.Clients.Ports;

public interface IClientDirectoryReader
{
    Task<ClientDirectoryReadPage> ReadPageAsync(
        ClientDirectoryReadRequest request,
        CancellationToken cancellationToken = default);
}

public enum ClientDirectorySort
{
    Code,
    DisplayName,
    LegalName,
    Status
}

public enum ClientDirectorySortDirection
{
    Ascending,
    Descending
}

public sealed record ClientDirectoryReadRequest(
    string Search,
    string? Status,
    ClientDirectorySort Sort,
    ClientDirectorySortDirection Direction,
    string? AfterSortValue,
    string? AfterCode,
    Guid? AfterClientId,
    int Take);

public sealed record ClientDirectoryReadPage(
    IReadOnlyCollection<ClientDirectoryReadItem> Items,
    long FilteredCount,
    ClientDirectoryReadSummary Summary);

public sealed record ClientDirectoryReadItem(
    Guid ClientId,
    string Code,
    string LegalName,
    string DisplayName,
    string Status,
    string SortValue);

public sealed record ClientDirectoryReadSummary(
    long TotalCount,
    long DraftCount,
    long ActiveCount,
    long SuspendedCount,
    long ArchivedCount);
