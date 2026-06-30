using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

public sealed class SurveyJobSettlement : ValueObject
{
    private SurveyJobSettlement(
        Money? lossAmount,
        Money? settledLaborAmount,
        Money? approvedPartsAmount,
        Money? policyDeductibleAmount,
        DateOnly? laborBillDate,
        Money? laborBillAmount,
        DateOnly? partsBillDate,
        Money? partsBillAmount,
        decimal? depreciationPercent,
        decimal? salvagePercent)
    {
        LossAmount = lossAmount;
        SettledLaborAmount = settledLaborAmount;
        ApprovedPartsAmount = approvedPartsAmount;
        PolicyDeductibleAmount = policyDeductibleAmount;
        LaborBillDate = laborBillDate;
        LaborBillAmount = laborBillAmount;
        PartsBillDate = partsBillDate;
        PartsBillAmount = partsBillAmount;
        DepreciationPercent = depreciationPercent;
        SalvagePercent = salvagePercent;
    }

    public Money? LossAmount { get; }

    public Money? SettledLaborAmount { get; }

    public Money? ApprovedPartsAmount { get; }

    public Money? PolicyDeductibleAmount { get; }

    public DateOnly? LaborBillDate { get; }

    public Money? LaborBillAmount { get; }

    public DateOnly? PartsBillDate { get; }

    public Money? PartsBillAmount { get; }

    public decimal? DepreciationPercent { get; }

    public decimal? SalvagePercent { get; }

    public Money? DepreciationAmount => CalculatePercentAmount(PartsBillAmount, DepreciationPercent);

    public Money? SalvageAmount => CalculatePercentAmount(PartsBillAmount, SalvagePercent);

    public static SurveyJobSettlement Create(
        Money? lossAmount = null,
        Money? settledLaborAmount = null,
        Money? approvedPartsAmount = null,
        Money? policyDeductibleAmount = null,
        DateOnly? laborBillDate = null,
        Money? laborBillAmount = null,
        DateOnly? partsBillDate = null,
        Money? partsBillAmount = null,
        decimal? depreciationPercent = null,
        decimal? salvagePercent = null)
    {
        EnsurePercent(depreciationPercent, nameof(depreciationPercent));
        EnsurePercent(salvagePercent, nameof(salvagePercent));

        return new SurveyJobSettlement(
            lossAmount,
            settledLaborAmount,
            approvedPartsAmount,
            policyDeductibleAmount,
            laborBillDate,
            laborBillAmount,
            partsBillDate,
            partsBillAmount,
            depreciationPercent,
            salvagePercent);
    }

    private static void EnsurePercent(decimal? percent, string parameterName)
    {
        if (percent is < 0 or > 100)
        {
            throw new ArgumentException("Percent must be between 0 and 100.", parameterName);
        }
    }

    private static Money? CalculatePercentAmount(Money? amount, decimal? percent)
    {
        return amount is null || percent is null
            ? null
            : Money.Of(amount.Amount * percent.Value / 100m, amount.CurrencyCode);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return LossAmount;
        yield return SettledLaborAmount;
        yield return ApprovedPartsAmount;
        yield return PolicyDeductibleAmount;
        yield return LaborBillDate;
        yield return LaborBillAmount;
        yield return PartsBillDate;
        yield return PartsBillAmount;
        yield return DepreciationPercent;
        yield return SalvagePercent;
    }
}
