namespace SafarSuite.ControlDesk.Application.Modules.Billing.CreateClientChargeRule;

public sealed record CreateClientChargeRuleCommand(
    Guid ClientId,
    Guid? ContractId,
    Guid ChargeCodeId,
    string? ProductModuleCode,
    string? DescriptionOverride,
    decimal UnitPriceAmount,
    string CurrencyCode,
    decimal Quantity,
    decimal TaxPercent,
    string BillingCycle,
    int BillingDayOfMonth,
    DateOnly EffectiveStartsOn,
    DateOnly EffectiveEndsOn);
