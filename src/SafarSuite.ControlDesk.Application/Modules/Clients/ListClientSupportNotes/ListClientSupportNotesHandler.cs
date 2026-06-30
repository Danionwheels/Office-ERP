using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.ListClientSupportNotes;

public sealed class ListClientSupportNotesHandler
{
    private readonly IClientRepository _clients;

    public ListClientSupportNotesHandler(IClientRepository clients)
    {
        _clients = clients;
    }

    public async Task<Result<ListClientSupportNotesResult>> HandleAsync(
        ListClientSupportNotesQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var clientId = ClientId.Create(query.ClientId);
            var client = await _clients.GetByIdAsync(clientId, cancellationToken);

            if (client is null)
            {
                return Result<ListClientSupportNotesResult>.Failure(ApplicationError.NotFound(
                    nameof(query.ClientId),
                    "Client was not found."));
            }

            var notes = client.SupportNotes
                .OrderByDescending(note => note.CreatedAtUtc)
                .Select(ClientResultMapper.ToSupportNoteResult)
                .ToArray();

            return Result<ListClientSupportNotesResult>.Success(new ListClientSupportNotesResult(
                client.Id.Value,
                notes));
        }
        catch (ArgumentException exception)
        {
            return Result<ListClientSupportNotesResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(query),
                exception.Message));
        }
    }
}
