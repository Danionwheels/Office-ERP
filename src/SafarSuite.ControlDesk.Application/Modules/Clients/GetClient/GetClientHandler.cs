using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.GetClient;

public sealed class GetClientHandler
{
    private readonly IClientRepository _clients;

    public GetClientHandler(IClientRepository clients)
    {
        _clients = clients;
    }

    public async Task<Result<ClientDetailsResult>> HandleAsync(
        GetClientQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await _clients.GetByIdAsync(
                ClientId.Create(query.ClientId),
                cancellationToken);

            if (client is null)
            {
                return Result<ClientDetailsResult>.Failure(ApplicationError.NotFound(
                    nameof(query.ClientId),
                    "Client was not found."));
            }

            return Result<ClientDetailsResult>.Success(ClientResultMapper.ToDetailsResult(client));
        }
        catch (ArgumentException exception)
        {
            return Result<ClientDetailsResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(query),
                exception.Message));
        }
    }
}
