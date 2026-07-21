using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryClientRepository : IClientRepository, IClientDirectoryReader
{
    private readonly ConcurrentDictionary<Guid, Client> _clientsById = new();

    public Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        _clientsById.TryAdd(client.Id.Value, client);

        return Task.CompletedTask;
    }

    public Task<Client?> GetByIdAsync(ClientId id, CancellationToken cancellationToken = default)
    {
        _clientsById.TryGetValue(id.Value, out var client);

        return Task.FromResult(client);
    }

    public Task<ClientDirectoryReadPage> ReadPageAsync(
        ClientDirectoryReadRequest request,
        CancellationToken cancellationToken = default)
    {
        var allClients = Snapshot();
        var summary = new ClientDirectoryReadSummary(
            allClients.LongLength,
            allClients.LongCount(client => client.Status == ClientStatus.Draft),
            allClients.LongCount(client => client.Status == ClientStatus.Active),
            allClients.LongCount(client => client.Status == ClientStatus.Suspended),
            allClients.LongCount(client => client.Status == ClientStatus.Archived));
        var filteredClients = allClients
            .Where(client => request.Status is null
                || string.Equals(client.Status.ToString(), request.Status, StringComparison.Ordinal))
            .Where(client => MatchesSearch(client, request.Search))
            .Select(client => new ClientDirectoryReadItem(
                client.Id.Value,
                client.Code.Value,
                client.LegalName,
                client.DisplayName,
                client.Status.ToString(),
                GetSortValue(client, request.Sort)))
            .ToArray();
        var filteredCount = filteredClients.LongLength;
        var orderedClients = Order(filteredClients, request.Direction);

        if (request.AfterClientId.HasValue)
        {
            orderedClients = orderedClients
                .Where(client => IsAfterCursor(client, request))
                .ToArray();
        }

        var page = orderedClients.Take(request.Take).ToArray();

        return Task.FromResult(new ClientDirectoryReadPage(page, filteredCount, summary));
    }

    public Task<bool> ExistsByCodeAsync(ClientCode code, CancellationToken cancellationToken = default)
    {
        var exists = _clientsById.Values.Any(client => client.Code.Equals(code));

        return Task.FromResult(exists);
    }

    internal Client[] Snapshot() => _clientsById.Values.ToArray();

    private static bool MatchesSearch(Client client, string search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        return $"{client.Code.Value} {client.DisplayName} {client.LegalName} {client.Status}"
            .Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetSortValue(Client client, ClientDirectorySort sort)
    {
        return sort switch
        {
            ClientDirectorySort.DisplayName => client.DisplayName,
            ClientDirectorySort.LegalName => client.LegalName,
            ClientDirectorySort.Status => client.Status.ToString(),
            _ => client.Code.Value
        };
    }

    private static ClientDirectoryReadItem[] Order(
        IEnumerable<ClientDirectoryReadItem> clients,
        ClientDirectorySortDirection direction)
    {
        return direction == ClientDirectorySortDirection.Descending
            ? clients
                .OrderByDescending(client => client.SortValue, StringComparer.Ordinal)
                .ThenByDescending(client => client.Code, StringComparer.Ordinal)
                .ThenByDescending(client => client.ClientId)
                .ToArray()
            : clients
                .OrderBy(client => client.SortValue, StringComparer.Ordinal)
                .ThenBy(client => client.Code, StringComparer.Ordinal)
                .ThenBy(client => client.ClientId)
                .ToArray();
    }

    private static bool IsAfterCursor(
        ClientDirectoryReadItem client,
        ClientDirectoryReadRequest request)
    {
        var comparison = string.Compare(
            client.SortValue,
            request.AfterSortValue,
            StringComparison.Ordinal);

        if (comparison == 0)
        {
            comparison = string.Compare(
                client.Code,
                request.AfterCode,
                StringComparison.Ordinal);
        }

        if (comparison == 0)
        {
            comparison = client.ClientId.CompareTo(request.AfterClientId!.Value);
        }

        return request.Direction == ClientDirectorySortDirection.Descending
            ? comparison < 0
            : comparison > 0;
    }
}
