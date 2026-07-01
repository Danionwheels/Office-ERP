using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Application.Modules.Contracts.GetClientContract;

public sealed class GetClientContractHandler
{
    private readonly IContractRepository _contracts;

    public GetClientContractHandler(IContractRepository contracts)
    {
        _contracts = contracts;
    }

    public async Task<Result<ClientContractResult>> HandleAsync(
        GetClientContractQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var contractId = ContractId.Create(query.ContractId);
            var contract = await _contracts.GetByIdAsync(contractId, cancellationToken);

            if (contract is null)
            {
                return Result<ClientContractResult>.Failure(ApplicationError.NotFound(
                    nameof(query.ContractId),
                    "Contract was not found."));
            }

            return Result<ClientContractResult>.Success(ContractResultMapper.ToResult(contract));
        }
        catch (ArgumentException exception)
        {
            return Result<ClientContractResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(query),
                exception.Message));
        }
    }
}
