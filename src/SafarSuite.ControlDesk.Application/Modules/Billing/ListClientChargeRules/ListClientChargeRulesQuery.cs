namespace SafarSuite.ControlDesk.Application.Modules.Billing.ListClientChargeRules;

public sealed record ListClientChargeRulesQuery(
    Guid ClientId,
    Guid? ContractId,
    DateOnly? EffectiveOn);
