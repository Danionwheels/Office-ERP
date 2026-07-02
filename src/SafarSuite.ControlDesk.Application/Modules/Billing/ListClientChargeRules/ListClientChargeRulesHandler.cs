using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.ListClientChargeRules;

public sealed class ListClientChargeRulesHandler
{
    private readonly IClientChargeRuleRepository _clientChargeRules;
    private readonly IClientRepository _clients;
    private readonly IClock _clock;

    public ListClientChargeRulesHandler(
        IClientChargeRuleRepository clientChargeRules,
        IClientRepository clients,
        IClock clock)
    {
        _clientChargeRules = clientChargeRules;
        _clients = clients;
        _clock = clock;
    }

    public async Task<Result<ListClientChargeRulesResult>> HandleAsync(
        ListClientChargeRulesQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var clientId = ClientId.Create(query.ClientId);

            if (await _clients.GetByIdAsync(clientId, cancellationToken) is null)
            {
                return Result<ListClientChargeRulesResult>.Failure(ApplicationError.NotFound(
                    nameof(query.ClientId),
                    "Client was not found."));
            }

            var contractId = query.ContractId.HasValue ? ContractId.Create(query.ContractId.Value) : (ContractId?)null;
            var effectiveOn = query.EffectiveOn ?? _clock.Today;
            var chargeRules = await _clientChargeRules.ListEffectiveForClientAsync(
                clientId,
                contractId,
                effectiveOn,
                cancellationToken);

            return Result<ListClientChargeRulesResult>.Success(new ListClientChargeRulesResult(
                effectiveOn,
                chargeRules.Select(ToResult).ToArray()));
        }
        catch (ArgumentException exception)
        {
            return Result<ListClientChargeRulesResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(query),
                exception.Message));
        }
    }

    private static ClientChargeRuleSummaryResult ToResult(ClientChargeRule chargeRule)
    {
        return new ClientChargeRuleSummaryResult(
            chargeRule.Id.Value,
            chargeRule.ClientId.Value,
            chargeRule.ContractId?.Value,
            chargeRule.ChargeCodeId.Value,
            chargeRule.ProductModuleCode?.Value,
            chargeRule.UnitPrice.Amount,
            chargeRule.UnitPrice.CurrencyCode,
            chargeRule.Quantity,
            chargeRule.TaxPercent,
            chargeRule.TaxAmount.Amount,
            chargeRule.LineAmount.Amount,
            chargeRule.TotalLineAmount.Amount,
            chargeRule.BillingCycle.ToString(),
            chargeRule.BillingDayOfMonth,
            chargeRule.EffectivePeriod.StartsOn,
            chargeRule.EffectivePeriod.EndsOn,
            chargeRule.Status.ToString());
    }
}
