namespace SafarSuite.ControlDesk.Application.Modules.Billing.CreateChargeCode;

public sealed record CreateChargeCodeCommand(
    string Code,
    string Name,
    string? Description,
    decimal DefaultUnitPriceAmount,
    string CurrencyCode,
    Guid RevenueAccountId,
    Guid? TaxAccountId);
