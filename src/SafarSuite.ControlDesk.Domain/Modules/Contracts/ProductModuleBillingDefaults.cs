namespace SafarSuite.ControlDesk.Domain.Modules.Contracts;

public sealed class ProductModuleBillingDefaults
{
    private ProductModuleBillingDefaults(
        string chargeCode,
        string chargeName,
        string description,
        decimal defaultUnitPriceAmount,
        string currencyCode,
        BillingCycle billingCycle)
    {
        ChargeCode = chargeCode;
        ChargeName = chargeName;
        Description = description;
        DefaultUnitPriceAmount = defaultUnitPriceAmount;
        CurrencyCode = currencyCode;
        BillingCycle = billingCycle;
    }

    public string ChargeCode { get; }

    public string ChargeName { get; }

    public string Description { get; }

    public decimal DefaultUnitPriceAmount { get; }

    public string CurrencyCode { get; }

    public BillingCycle BillingCycle { get; }

    public static ProductModuleBillingDefaults Create(
        string chargeCode,
        string chargeName,
        string description,
        decimal defaultUnitPriceAmount,
        string currencyCode,
        BillingCycle billingCycle)
    {
        if (string.IsNullOrWhiteSpace(chargeCode))
        {
            throw new ArgumentException("Module billing charge code is required.", nameof(chargeCode));
        }

        var cleanedChargeCode = chargeCode.Trim().ToUpperInvariant();

        if (cleanedChargeCode.Length is < 2 or > 32)
        {
            throw new ArgumentException("Module billing charge code must be between 2 and 32 characters.", nameof(chargeCode));
        }

        if (string.IsNullOrWhiteSpace(chargeName))
        {
            throw new ArgumentException("Module billing charge name is required.", nameof(chargeName));
        }

        var cleanedChargeName = chargeName.Trim();

        if (cleanedChargeName.Length > 128)
        {
            throw new ArgumentException("Module billing charge name cannot exceed 128 characters.", nameof(chargeName));
        }

        var cleanedDescription = description.Trim();

        if (cleanedDescription.Length > 256)
        {
            throw new ArgumentException("Module billing description cannot exceed 256 characters.", nameof(description));
        }

        if (defaultUnitPriceAmount < 0)
        {
            throw new ArgumentException("Module billing default unit price cannot be negative.", nameof(defaultUnitPriceAmount));
        }

        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("Module billing currency code is required.", nameof(currencyCode));
        }

        var cleanedCurrencyCode = currencyCode.Trim().ToUpperInvariant();

        if (cleanedCurrencyCode.Length != 3)
        {
            throw new ArgumentException("Module billing currency code must be 3 characters.", nameof(currencyCode));
        }

        if (!Enum.IsDefined(billingCycle))
        {
            throw new ArgumentException("Module billing cycle is invalid.", nameof(billingCycle));
        }

        return new ProductModuleBillingDefaults(
            cleanedChargeCode,
            cleanedChargeName,
            cleanedDescription,
            decimal.Round(defaultUnitPriceAmount, 2),
            cleanedCurrencyCode,
            billingCycle);
    }
}
