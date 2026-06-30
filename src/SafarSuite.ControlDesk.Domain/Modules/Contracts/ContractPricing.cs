using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Contracts;

public sealed class ContractPricing : ValueObject
{
    private ContractPricing(Money recurringAmount, BillingCycle billingCycle, int billingDayOfMonth)
    {
        RecurringAmount = recurringAmount;
        BillingCycle = billingCycle;
        BillingDayOfMonth = billingDayOfMonth;
    }

    public Money RecurringAmount { get; }

    public BillingCycle BillingCycle { get; }

    public int BillingDayOfMonth { get; }

    public static ContractPricing Create(Money recurringAmount, BillingCycle billingCycle, int billingDayOfMonth)
    {
        if (billingDayOfMonth is < 1 or > 28)
        {
            throw new ArgumentException("Billing day must be between 1 and 28.", nameof(billingDayOfMonth));
        }

        return new ContractPricing(recurringAmount, billingCycle, billingDayOfMonth);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RecurringAmount;
        yield return BillingCycle;
        yield return BillingDayOfMonth;
    }
}
