using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.ListClientContacts;

public sealed class ListClientContactsHandler
{
    private readonly IClientRepository _clients;

    public ListClientContactsHandler(IClientRepository clients)
    {
        _clients = clients;
    }

    public async Task<Result<ListClientContactsResult>> HandleAsync(
        ListClientContactsQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var clientId = ClientId.Create(query.ClientId);
            var client = await _clients.GetByIdAsync(clientId, cancellationToken);

            if (client is null)
            {
                return Result<ListClientContactsResult>.Failure(ApplicationError.NotFound(
                    nameof(query.ClientId),
                    "Client was not found."));
            }

            var contacts = client.Contacts
                .OrderByDescending(contact => contact.IsPrimary)
                .ThenBy(contact => contact.Role)
                .ThenBy(contact => contact.FullName)
                .Select(ClientResultMapper.ToContactResult)
                .ToArray();

            return Result<ListClientContactsResult>.Success(new ListClientContactsResult(
                client.Id.Value,
                contacts));
        }
        catch (ArgumentException exception)
        {
            return Result<ListClientContactsResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(query),
                exception.Message));
        }
    }
}
