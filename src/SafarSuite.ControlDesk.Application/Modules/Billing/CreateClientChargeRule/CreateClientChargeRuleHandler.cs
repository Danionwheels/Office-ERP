using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.CreateClientChargeRule;

public sealed class CreateClientChargeRuleHandler
{
    private readonly IClientChargeRuleRepository _clientChargeRules;
    private readonly IChargeCodeRepository _chargeCodes;
    private readonly IClientRepository _clients;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly CreateClientChargeRuleValidator _validator;

    public CreateClientChargeRuleHandler(
        IClientChargeRuleRepository clientChargeRules,
        IChargeCodeRepository chargeCodes,
        IClientRepository clients,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        CreateClientChargeRuleValidator validator)
    {
        _clientChargeRules = clientChargeRules;
        _chargeCodes = chargeCodes;
        _clients = clients;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<CreateClientChargeRuleResult>> HandleAsync(
        CreateClientChargeRuleCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<CreateClientChargeRuleResult>.Failure(validationErrors);
        }

        try
        {
            var clientId = ClientId.Create(command.ClientId);

            if (await _clients.GetByIdAsync(clientId, cancellationToken) is null)
            {
                return Result<CreateClientChargeRuleResult>.Failure(ApplicationError.NotFound(
                    nameof(command.ClientId),
                    "Client was not found."));
            }

            var chargeCodeId = ChargeCodeId.Create(command.ChargeCodeId);

            if (await _chargeCodes.GetByIdAsync(chargeCodeId, cancellationToken) is null)
            {
                return Result<CreateClientChargeRuleResult>.Failure(ApplicationError.NotFound(
                    nameof(command.ChargeCodeId),
                    "Charge code was not found."));
            }

            if (!Enum.TryParse<BillingCycle>(command.BillingCycle, true, out var billingCycle))
            {
                return Result<CreateClientChargeRuleResult>.Failure(ApplicationError.Validation(
                    nameof(command.BillingCycle),
                    "Billing cycle is invalid."));
            }

            var clientChargeRule = ClientChargeRule.Create(
                ClientChargeRuleId.Create(_idGenerator.NewGuid()),
                clientId,
                command.ContractId.HasValue ? ContractId.Create(command.ContractId.Value) : null,
                chargeCodeId,
                command.DescriptionOverride,
                Money.Of(command.UnitPriceAmount, command.CurrencyCode),
                command.Quantity,
                command.TaxPercent,
                billingCycle,
                command.BillingDayOfMonth,
                DateRange.Create(command.EffectiveStartsOn, command.EffectiveEndsOn),
                _clock.UtcNow);

            await _clientChargeRules.AddAsync(clientChargeRule, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<CreateClientChargeRuleResult>.Success(new CreateClientChargeRuleResult(
                clientChargeRule.Id.Value,
                clientChargeRule.ClientId.Value,
                clientChargeRule.ContractId?.Value,
                clientChargeRule.ChargeCodeId.Value,
                clientChargeRule.UnitPrice.Amount,
                clientChargeRule.UnitPrice.CurrencyCode,
                clientChargeRule.Quantity,
                clientChargeRule.TaxPercent,
                clientChargeRule.TaxAmount.Amount,
                clientChargeRule.LineAmount.Amount,
                clientChargeRule.TotalLineAmount.Amount,
                clientChargeRule.BillingCycle.ToString(),
                clientChargeRule.BillingDayOfMonth,
                clientChargeRule.EffectivePeriod.StartsOn,
                clientChargeRule.EffectivePeriod.EndsOn,
                clientChargeRule.Status.ToString()));
        }
        catch (ArgumentException exception)
        {
            return Result<CreateClientChargeRuleResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }
}
