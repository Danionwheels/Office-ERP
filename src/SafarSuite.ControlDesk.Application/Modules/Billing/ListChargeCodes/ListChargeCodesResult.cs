namespace SafarSuite.ControlDesk.Application.Modules.Billing.ListChargeCodes;

public sealed record ListChargeCodesResult(
    IReadOnlyCollection<ChargeCodeLookupResult> ChargeCodes);

public sealed record ChargeCodeLookupResult(
    Guid ChargeCodeId,
    string Code,
    string Name,
    decimal DefaultUnitPriceAmount,
    string CurrencyCode,
    Guid RevenueAccountId,
    Guid? TaxAccountId,
    string Status);
