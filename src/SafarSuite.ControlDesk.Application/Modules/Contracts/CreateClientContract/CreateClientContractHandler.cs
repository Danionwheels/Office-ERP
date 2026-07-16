using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Application.Modules.Contracts;
using SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Application.Modules.Contracts.CreateClientContract;

public sealed class CreateClientContractHandler
{
    private readonly IClientRepository _clients;
    private readonly IContractRepository _contracts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly CreateClientContractValidator _validator;
    private readonly ProductModuleSelectionService _moduleSelection;

    public CreateClientContractHandler(
        IClientRepository clients,
        IContractRepository contracts,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        CreateClientContractValidator validator,
        ProductModuleSelectionService moduleSelection)
    {
        _clients = clients;
        _contracts = contracts;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
        _validator = validator;
        _moduleSelection = moduleSelection;
    }

    public async Task<Result<CreateClientContractResult>> HandleAsync(
        CreateClientContractCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<CreateClientContractResult>.Failure(validationErrors);
        }

        try
        {
            var clientId = ClientId.Create(command.ClientId);
            var contractNumber = ContractNumber.Create(command.ContractNumber);

            if (await _clients.GetByIdAsync(clientId, cancellationToken) is null)
            {
                return Result<CreateClientContractResult>.Failure(ApplicationError.NotFound(
                    nameof(command.ClientId),
                    "Client was not found."));
            }

            if (await _contracts.ExistsByNumberAsync(contractNumber, cancellationToken))
            {
                return Result<CreateClientContractResult>.Failure(ApplicationError.Conflict(
                    nameof(command.ContractNumber),
                    $"Contract {contractNumber.Value} already exists."));
            }

            if (!Enum.TryParse<BillingCycle>(command.BillingCycle, ignoreCase: true, out var billingCycle)
                || !Enum.IsDefined(billingCycle))
            {
                return Result<CreateClientContractResult>.Failure(ApplicationError.Validation(
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
                return Result<CreateClientContractResult>.Failure(moduleAllowances.Errors);
            }

            var featureLimits = (command.FeatureLimits ?? [])
                .Select(limit => ModuleFeatureLimit.Create(
                    limit.ModuleCode,
                    limit.FeatureCode,
                    limit.LimitValue,
                    limit.Unit))
                .ToArray();

            var contract = await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var latest = await _contracts.GetLatestForClientForUpdateAsync(clientId, token);

                    if (latest is not null)
                    {
                        return null;
                    }

                    var approvedAtUtc = _clock.UtcNow;
                    var created = ClientContract.Create(
                        ContractId.Create(_idGenerator.NewGuid()),
                        clientId,
                        revisionNumber: 1,
                        supersedesContractId: null,
                        moduleAllowances.Value.CatalogRevisionId,
                        moduleAllowances.Value.CatalogRevisionNumber,
                        contractNumber,
                        DateRange.Create(command.StartsOn, command.EndsOn),
                        ContractPricing.Create(
                            Money.Of(command.RecurringAmount, command.CurrencyCode),
                            billingCycle,
                            command.BillingDayOfMonth),
                        DeviceAllowance.Create(command.AllowedDevices),
                        BranchAllowance.Create(command.AllowedBranches),
                        command.ApprovedBy,
                        command.ApprovalReason,
                        approvedAtUtc,
                        approvedAtUtc,
                        UserAllowance.Create(
                            command.AllowedNamedUsers,
                            command.AllowedConcurrentUsers),
                        featureLimits);

                    foreach (var moduleAllowance in moduleAllowances.Value.Allowances)
                    {
                        created.SetModuleAllowance(moduleAllowance);
                    }

                    created.Activate(approvedAtUtc);
                    await _contracts.AddAsync(created, token);

                    return created;
                },
                cancellationToken);

            if (contract is null)
            {
                return Result<CreateClientContractResult>.Failure(ApplicationError.Conflict(
                    nameof(command.ClientId),
                    "Client already has contract history. Create a replacement revision instead."));
            }

            return Result<CreateClientContractResult>.Success(ToResult(contract));
        }
        catch (ArgumentException exception)
        {
            return Result<CreateClientContractResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<CreateClientContractResult>.Failure(ApplicationError.Validation(
                nameof(command),
                exception.Message));
        }
    }

    private static CreateClientContractResult ToResult(ClientContract contract)
    {
        return new CreateClientContractResult(
            contract.Id.Value,
            contract.ClientId.Value,
            contract.RevisionNumber,
            contract.SupersedesContractId?.Value,
            contract.ProductCatalogRevisionId.Value,
            contract.ProductCatalogRevisionNumber,
            contract.Number.Value,
            contract.Term.StartsOn,
            contract.Term.EndsOn,
            contract.Pricing.RecurringAmount.Amount,
            contract.Pricing.RecurringAmount.CurrencyCode,
            contract.Pricing.BillingCycle.ToString(),
            contract.Pricing.BillingDayOfMonth,
            contract.DeviceAllowance.AllowedDevices,
            contract.BranchAllowance.AllowedBranches,
            contract.Status.ToString(),
            contract.CreatedAtUtc,
            contract.ActivatedAtUtc,
            contract.ApprovedBy,
            contract.ApprovalReason,
            contract.ApprovedAtUtc,
            contract.ModuleAllowances.Select(module => new CreateClientContractModuleResult(
                module.ModuleCode.Value,
                module.IsEnabled)).ToArray(),
            contract.UserAllowance.AllowedNamedUsers,
            contract.UserAllowance.AllowedConcurrentUsers,
            contract.FeatureLimits.Select(limit => new CreateClientContractFeatureLimitResult(
                limit.ModuleCode.Value,
                limit.FeatureCode.Value,
                limit.LimitValue,
                limit.Unit)).ToArray());
    }
}
