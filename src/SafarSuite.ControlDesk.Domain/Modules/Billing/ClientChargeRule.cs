using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Billing;

public sealed class ClientChargeRule : Entity<ClientChargeRuleId>
{
    private ClientChargeRule(
        ClientChargeRuleId id,
        ClientId clientId,
        ContractId? contractId,
        ChargeCodeId chargeCodeId,
        string? descriptionOverride,
        Money unitPrice,
        decimal quantity,
        BillingCycle billingCycle,
        int billingDayOfMonth,
        DateRange effectivePeriod,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        ClientId = clientId;
        ContractId = contractId;
        ChargeCodeId = chargeCodeId;
        DescriptionOverride = descriptionOverride;
        UnitPrice = unitPrice;
        Quantity = quantity;
        BillingCycle = billingCycle;
        BillingDayOfMonth = billingDayOfMonth;
        EffectivePeriod = effectivePeriod;
        CreatedAtUtc = createdAtUtc;
        Status = ClientChargeRuleStatus.Active;
    }

    public ClientId ClientId { get; }

    public ContractId? ContractId { get; }

    public ChargeCodeId ChargeCodeId { get; }

    public string? DescriptionOverride { get; private set; }

    public Money UnitPrice { get; private set; }

    public decimal Quantity { get; private set; }

    public BillingCycle BillingCycle { get; private set; }

    public int BillingDayOfMonth { get; private set; }

    public DateRange EffectivePeriod { get; private set; }

    public ClientChargeRuleStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public Money LineAmount => Money.Of(UnitPrice.Amount * Quantity, UnitPrice.CurrencyCode);

    public static ClientChargeRule Create(
        ClientChargeRuleId id,
        ClientId clientId,
        ContractId? contractId,
        ChargeCodeId chargeCodeId,
        string? descriptionOverride,
        Money unitPrice,
        decimal quantity,
        BillingCycle billingCycle,
        int billingDayOfMonth,
        DateRange effectivePeriod,
        DateTimeOffset createdAtUtc)
    {
        ValidatePriceAndQuantity(unitPrice, quantity);
        ValidateBillingDay(billingDayOfMonth);

        return new ClientChargeRule(
            id,
            clientId,
            contractId,
            chargeCodeId,
            CleanText(descriptionOverride),
            unitPrice,
            decimal.Round(quantity, 4),
            billingCycle,
            billingDayOfMonth,
            effectivePeriod,
            createdAtUtc);
    }

    public void UpdatePricing(Money unitPrice, decimal quantity)
    {
        ValidatePriceAndQuantity(unitPrice, quantity);

        UnitPrice = unitPrice;
        Quantity = decimal.Round(quantity, 4);
    }

    public void UpdateBillingSchedule(BillingCycle billingCycle, int billingDayOfMonth)
    {
        ValidateBillingDay(billingDayOfMonth);

        BillingCycle = billingCycle;
        BillingDayOfMonth = billingDayOfMonth;
    }

    public void UpdateEffectivePeriod(DateRange effectivePeriod)
    {
        EffectivePeriod = effectivePeriod;
    }

    public void UpdateDescriptionOverride(string? descriptionOverride)
    {
        DescriptionOverride = CleanText(descriptionOverride);
    }

    public void Activate()
    {
        Status = ClientChargeRuleStatus.Active;
    }

    public void Deactivate()
    {
        Status = ClientChargeRuleStatus.Inactive;
    }

    public bool IsEffectiveOn(DateOnly date)
    {
        return Status == ClientChargeRuleStatus.Active && EffectivePeriod.Contains(date);
    }

    private static void ValidatePriceAndQuantity(Money unitPrice, decimal quantity)
    {
        if (unitPrice.Amount < 0)
        {
            throw new ArgumentException("Client charge unit price cannot be negative.", nameof(unitPrice));
        }

        if (quantity <= 0)
        {
            throw new ArgumentException("Client charge quantity must be positive.", nameof(quantity));
        }
    }

    private static void ValidateBillingDay(int billingDayOfMonth)
    {
        if (billingDayOfMonth is < 1 or > 28)
        {
            throw new ArgumentException("Billing day must be between 1 and 28.", nameof(billingDayOfMonth));
        }
    }

    private static string? CleanText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
