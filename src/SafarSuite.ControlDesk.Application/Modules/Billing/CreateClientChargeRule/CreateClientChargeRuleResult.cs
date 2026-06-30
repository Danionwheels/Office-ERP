namespace SafarSuite.ControlDesk.Application.Modules.Billing.CreateClientChargeRule;

public sealed record CreateClientChargeRuleResult(
    Guid ClientChargeRuleId,
    Guid ClientId,
    Guid? ContractId,
    Guid ChargeCodeId,
    decimal UnitPriceAmount,
    string CurrencyCode,
    decimal Quantity,
    decimal LineAmount,
    string BillingCycle,
    int BillingDayOfMonth,
    DateOnly EffectiveStartsOn,
    DateOnly EffectiveEndsOn,
    string Status);
