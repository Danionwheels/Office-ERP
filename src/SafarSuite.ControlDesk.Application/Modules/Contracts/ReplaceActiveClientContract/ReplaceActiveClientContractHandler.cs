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

            var featureLimits = (command.FeatureLimits ?? [])
                .Select(limit => ModuleFeatureLimit.Create(
                    limit.ModuleCode,
                    limit.FeatureCode,
                    limit.LimitValue,
                    limit.Unit))
                .ToArray();

            var result = await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var latestContract = await _contracts.GetLatestForClientForUpdateAsync(clientId, token);
                    var existingActiveContract = await _contracts.GetActiveForClientAsync(clientId, token);
                    existingActiveContract?.Suspend();

                    var approvedAtUtc = _clock.UtcNow;
                    var contract = ClientContract.Create(
                        ContractId.Create(_idGenerator.NewGuid()),
                        clientId,
                        (latestContract?.RevisionNumber ?? 0) + 1,
                        latestContract?.Id,
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
                        contract.SetModuleAllowance(moduleAllowance);
                    }

                    contract.Activate(approvedAtUtc);

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

        ValidateUserAndFeatureLimits(
            command.AllowedNamedUsers,
            command.AllowedConcurrentUsers,
            command.FeatureLimits ?? [],
            errors);

        if (string.IsNullOrWhiteSpace(command.ApprovedBy))
        {
            errors.Add(ApplicationError.Validation(nameof(command.ApprovedBy), "Contract approver is required."));
        }
        else if (command.ApprovedBy.Trim().Length > 256)
        {
            errors.Add(ApplicationError.Validation(nameof(command.ApprovedBy), "Contract approver cannot exceed 256 characters."));
        }

        if (string.IsNullOrWhiteSpace(command.ApprovalReason))
        {
            errors.Add(ApplicationError.Validation(nameof(command.ApprovalReason), "Contract approval reason is required."));
        }
        else if (command.ApprovalReason.Trim().Length > 1000)
        {
            errors.Add(ApplicationError.Validation(nameof(command.ApprovalReason), "Contract approval reason cannot exceed 1000 characters."));
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

    private static void ValidateUserAndFeatureLimits(
        int? allowedNamedUsers,
        int? allowedConcurrentUsers,
        IReadOnlyCollection<ReplaceActiveClientContractFeatureLimitCommand> featureLimits,
        ICollection<ApplicationError> errors)
    {
        if (allowedNamedUsers < 0)
        {
            errors.Add(ApplicationError.Validation(
                nameof(allowedNamedUsers),
                "Allowed named-user count cannot be negative."));
        }

        if (allowedConcurrentUsers < 0)
        {
            errors.Add(ApplicationError.Validation(
                nameof(allowedConcurrentUsers),
                "Allowed concurrent-user count cannot be negative."));
        }

        if (allowedNamedUsers.HasValue
            && allowedConcurrentUsers.HasValue
            && allowedConcurrentUsers.Value > allowedNamedUsers.Value)
        {
            errors.Add(ApplicationError.Validation(
                nameof(allowedConcurrentUsers),
                "Allowed concurrent-user count cannot exceed the named-user count."));
        }

        foreach (var limit in featureLimits)
        {
            if (string.IsNullOrWhiteSpace(limit.ModuleCode))
            {
                errors.Add(ApplicationError.Validation(nameof(limit.ModuleCode), "Feature-limit module code is required."));
            }

            if (string.IsNullOrWhiteSpace(limit.FeatureCode))
            {
                errors.Add(ApplicationError.Validation(nameof(limit.FeatureCode), "Feature code is required."));
            }

            if (limit.LimitValue < 0)
            {
                errors.Add(ApplicationError.Validation(nameof(limit.LimitValue), "Feature limit value cannot be negative."));
            }

            if (string.IsNullOrWhiteSpace(limit.Unit))
            {
                errors.Add(ApplicationError.Validation(nameof(limit.Unit), "Feature limit unit is required."));
            }
        }
    }
}
