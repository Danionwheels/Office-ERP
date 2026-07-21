namespace SafarSuite.ControlDesk.Application.Modules.Clients.ListClients;

public sealed record ListClientsResult(
    IReadOnlyCollection<ClientLookupResult> Clients,
    int PageSize,
    bool HasMore,
    string? NextCursor,
    long FilteredCount,
    ClientDirectorySummaryResult Summary);

public sealed record ClientLookupResult(
    Guid ClientId,
    string Code,
    string LegalName,
    string DisplayName,
    string Status);

public sealed record ClientDirectorySummaryResult(
    long TotalCount,
    long DraftCount,
    long ActiveCount,
    long SuspendedCount,
    long ArchivedCount);
