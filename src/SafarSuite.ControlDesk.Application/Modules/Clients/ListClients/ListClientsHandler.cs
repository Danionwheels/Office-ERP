using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.ListClients;

public sealed class ListClientsHandler
{
    private readonly IClientRepository _clients;

    public ListClientsHandler(IClientRepository clients)
    {
        _clients = clients;
    }

    public async Task<Result<ListClientsResult>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var clients = await _clients.ListAsync(cancellationToken);

        return Result<ListClientsResult>.Success(new ListClientsResult(
            clients.Select(client => new ClientLookupResult(
                client.Id.Value,
                client.Code.Value,
                client.LegalName,
                client.DisplayName,
                client.Status.ToString())).ToArray()));
    }
}
