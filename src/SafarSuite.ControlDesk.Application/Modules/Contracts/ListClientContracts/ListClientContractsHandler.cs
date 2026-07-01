using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Contracts.ListClientContracts;

public sealed class ListClientContractsHandler
{
    private readonly IClientRepository _clients;
    private readonly IContractRepository _contracts;

    public ListClientContractsHandler(
        IClientRepository clients,
        IContractRepository contracts)
    {
        _clients = clients;
        _contracts = contracts;
    }

    public async Task<Result<ListClientContractsResult>> HandleAsync(
        ListClientContractsQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var clientId = ClientId.Create(query.ClientId);

            if (await _clients.GetByIdAsync(clientId, cancellationToken) is null)
            {
                return Result<ListClientContractsResult>.Failure(ApplicationError.NotFound(
                    nameof(query.ClientId),
                    "Client was not found."));
            }

            var contracts = await _contracts.ListForClientAsync(clientId, cancellationToken);

            return Result<ListClientContractsResult>.Success(new ListClientContractsResult(
                clientId.Value,
                contracts.Select(ContractResultMapper.ToResult).ToArray()));
        }
        catch (ArgumentException exception)
        {
            return Result<ListClientContractsResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(query),
                exception.Message));
        }
    }
}
