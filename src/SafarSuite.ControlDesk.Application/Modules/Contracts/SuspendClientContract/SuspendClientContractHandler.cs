using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Application.Modules.Contracts.SuspendClientContract;

public sealed class SuspendClientContractHandler
{
    private readonly IContractRepository _contracts;
    private readonly IUnitOfWork _unitOfWork;

    public SuspendClientContractHandler(
        IContractRepository contracts,
        IUnitOfWork unitOfWork)
    {
        _contracts = contracts;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ClientContractResult>> HandleAsync(
        SuspendClientContractCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var contractId = ContractId.Create(command.ContractId);
            var contract = await _contracts.GetByIdAsync(contractId, cancellationToken);

            if (contract is null)
            {
                return Result<ClientContractResult>.Failure(ApplicationError.NotFound(
                    nameof(command.ContractId),
                    "Contract was not found."));
            }

            if (contract.Status != ContractStatus.Active)
            {
                return Result<ClientContractResult>.Failure(ApplicationError.Validation(
                    nameof(command.ContractId),
                    "Only active contracts can be suspended."));
            }

            contract.Suspend();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<ClientContractResult>.Success(ContractResultMapper.ToResult(contract));
        }
        catch (ArgumentException exception)
        {
            return Result<ClientContractResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }
}
