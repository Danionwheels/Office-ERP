namespace SafarSuite.ControlDesk.Application.Modules.Billing.CreateClientChargeRule;

public sealed record CreateClientChargeRuleResult(
    Guid ClientChargeRuleId,
    Guid ClientId,
    Guid? ContractId,
    Guid ChargeCodeId,
    string? ProductModuleCode,
    decimal UnitPriceAmount,
    string CurrencyCode,
    decimal Quantity,
    decimal TaxPercent,
    decimal TaxAmount,
    decimal LineAmount,
    decimal TotalLineAmount,
    string BillingCycle,
    int BillingDayOfMonth,
    DateOnly EffectiveStartsOn,
    DateOnly EffectiveEndsOn,
    string Status);
