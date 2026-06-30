using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

public sealed class PolicyLossDetails : ValueObject
{
    private PolicyLossDetails(string? lossNumber, string? policyNumber, string? purchaseOrderNumber)
    {
        LossNumber = lossNumber;
        PolicyNumber = policyNumber;
        PurchaseOrderNumber = purchaseOrderNumber;
    }

    public string? LossNumber { get; }

    public string? PolicyNumber { get; }

    public string? PurchaseOrderNumber { get; }

    public static PolicyLossDetails Create(
        string? lossNumber = null,
        string? policyNumber = null,
        string? purchaseOrderNumber = null)
    {
        return new PolicyLossDetails(Clean(lossNumber), Clean(policyNumber), Clean(purchaseOrderNumber));
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return LossNumber;
        yield return PolicyNumber;
        yield return PurchaseOrderNumber;
    }
}
