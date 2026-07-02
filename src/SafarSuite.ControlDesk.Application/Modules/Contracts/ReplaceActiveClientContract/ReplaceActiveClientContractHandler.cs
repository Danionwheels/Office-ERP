using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Application.Modules.Contracts;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Application.Modules.Contracts.ReplaceActiveClientContract;

public sealed class ReplaceActiveClientContractHandler
{
    private readonly IClientRepository _clients;
    private readonly IContractRepository _contracts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly ProductModuleSelectionService _moduleSelection;

    public ReplaceActiveClientContractHandler(
        IClientRepository clients,
        IContractRepository contracts,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        ProductModuleSelectionService moduleSelection)
    {
        _clients = clients;
        _contracts = contracts;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
        _moduleSelection = moduleSelection;
    }

    public async Task<Result<ReplaceActiveClientContractResult>> HandleAsync(
        ReplaceActiveClientContractCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<ReplaceActiveClientContractResult>.Failure(validationErrors);
        }

        try
        {
            var clientId = ClientId.Create(command.ClientId);
            var contractNumber = ContractNumber.Create(command.ContractNumber);

            if (await _clients.GetByIdAsync(clientId, cancellationToken) is null)
            {
                return Result<ReplaceActiveClientContractResult>.Failure(ApplicationError.NotFound(
                    nameof(command.ClientId),
                    "Client was not found."));
            }

            if (await _contracts.ExistsByNumberAsync(contractNumber, cancellationToken))
            {
                return Result<ReplaceActiveClientContractResult>.Failure(ApplicationError.Conflict(
                    nameof(command.ContractNumber),
                    $"Contract {contractNumber.Value} already exists."));
            }

            if (!Enum.TryParse<BillingCycle>(command.BillingCycle, ignoreCase: true, out var billingCycle)
                || !Enum.IsDefined(billingCycle))
            {
                return Result<ReplaceActiveClientContractResult>.Failure(ApplicationError.Validation(
                    nameof(command.BillingCycle),
                    "Billing cycle is invalid."));
            }

            var moduleAllowances = await _moduleSelection.BuildAllowancesAsync(
                command.Modules.Select(module => new ProductModuleSelection(
                        module.ModuleCode,
                        module.IsEnabled))
                    .ToArray(),
                cancellationToken);

            if (moduleAllowances.IsFailure)
            {
                return Result<ReplaceActiveClientContractResult>.Failure(moduleAllowances.Errors);
            }

            var result = await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var existingActiveContract = await _contracts.GetActiveForClientAsync(clientId, token);
                    existingActiveContract?.Suspend();

                    var contract = ClientContract.Create(
                        ContractId.Create(_idGenerator.NewGuid()),
                        clientId,
                        contractNumber,
                        DateRange.Create(command.StartsOn, command.EndsOn),
                        ContractPricing.Create(
                            Money.Of(command.RecurringAmount, command.CurrencyCode),
                            billingCycle,
                            command.BillingDayOfMonth),
                        DeviceAllowance.Create(command.AllowedDevices),
                        BranchAllowance.Create(command.AllowedBranches),
                        _clock.UtcNow);

                    foreach (var moduleAllowance in moduleAllowances.Value)
                    {
                        contract.SetModuleAllowance(moduleAllowance);
                    }

                    contract.Activate(_clock.UtcNow);

                    await _contracts.AddAsync(contract, token);

                    return new ReplaceActiveClientContractResult(
                        existingActiveContract is null ? null : ContractResultMapper.ToResult(existingActiveContract),
                        ContractResultMapper.ToResult(contract));
                },
                cancellationToken);

            return Result<ReplaceActiveClientContractResult>.Success(result);
        }
        catch (ArgumentException exception)
        {
            return Result<ReplaceActiveClientContractResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<ReplaceActiveClientContractResult>.Failure(ApplicationError.Validation(
                nameof(command),
                exception.Message));
        }
    }

    private static IReadOnlyCollection<ApplicationError> Validate(ReplaceActiveClientContractCommand command)
    {
        var errors = new List<ApplicationError>();

        if (command.ClientId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(command.ClientId), "Client id is required."));
        }

        if (string.IsNullOrWhiteSpace(command.ContractNumber))
        {
            errors.Add(ApplicationError.Validation(nameof(command.ContractNumber), "Contract number is required."));
        }

        if (command.EndsOn < command.StartsOn)
        {
            errors.Add(ApplicationError.Validation(nameof(command.EndsOn), "Contract end date cannot be before start date."));
        }

        if (command.RecurringAmount < 0)
        {
            errors.Add(ApplicationError.Validation(nameof(command.RecurringAmount), "Recurring amount cannot be negative."));
        }

        if (command.BillingDayOfMonth is < 1 or > 28)
        {
            errors.Add(ApplicationError.Validation(nameof(command.BillingDayOfMonth), "Billing day must be between 1 and 28."));
        }

        if (command.AllowedDevices < 0)
        {
            errors.Add(ApplicationError.Validation(nameof(command.AllowedDevices), "Allowed device count cannot be negative."));
        }

        if (command.AllowedBranches < 0)
        {
            errors.Add(ApplicationError.Validation(nameof(command.AllowedBranches), "Allowed branch count cannot be negative."));
        }

        foreach (var module in command.Modules)
        {
            if (string.IsNullOrWhiteSpace(module.ModuleCode))
            {
                errors.Add(ApplicationError.Validation(nameof(module.ModuleCode), "Module code is required."));
            }
        }

        return errors;
    }
}
