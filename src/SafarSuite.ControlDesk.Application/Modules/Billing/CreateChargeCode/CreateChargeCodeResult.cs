namespace SafarSuite.ControlDesk.Application.Modules.Billing.CreateChargeCode;

public sealed record CreateChargeCodeResult(
    Guid ChargeCodeId,
    string Code,
    string Name,
    decimal DefaultUnitPriceAmount,
    string CurrencyCode,
    Guid RevenueAccountId,
    Guid? TaxAccountId,
    string Status);
