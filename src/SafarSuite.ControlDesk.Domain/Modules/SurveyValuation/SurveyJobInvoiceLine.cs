using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

public sealed class SurveyJobInvoiceLine : ValueObject
{
    private SurveyJobInvoiceLine(
        int sequenceNumber,
        SurveyInvoiceLineDescriptionType descriptionType,
        string description,
        Money amount,
        SurveyReferenceCode? billingHeadCode,
        SurveyReferenceCode? taxCode,
        SurveyReferenceCode? categoryCode)
    {
        SequenceNumber = sequenceNumber;
        DescriptionType = descriptionType;
        Description = description;
        Amount = amount;
        BillingHeadCode = billingHeadCode;
        TaxCode = taxCode;
        CategoryCode = categoryCode;
    }

    public int SequenceNumber { get; }

    public SurveyInvoiceLineDescriptionType DescriptionType { get; }

    public string Description { get; }

    public Money Amount { get; }

    public SurveyReferenceCode? BillingHeadCode { get; }

    public SurveyReferenceCode? TaxCode { get; }

    public SurveyReferenceCode? CategoryCode { get; }

    public static SurveyJobInvoiceLine Create(
        int sequenceNumber,
        SurveyInvoiceLineDescriptionType descriptionType,
        string description,
        Money amount,
        SurveyReferenceCode? billingHeadCode = null,
        SurveyReferenceCode? taxCode = null,
        SurveyReferenceCode? categoryCode = null)
    {
        if (sequenceNumber <= 0)
        {
            throw new ArgumentException("Sequence number must be positive.", nameof(sequenceNumber));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Invoice line description is required.", nameof(description));
        }

        if (amount.Amount < 0)
        {
            throw new ArgumentException("Invoice line amount cannot be negative.", nameof(amount));
        }

        return new SurveyJobInvoiceLine(
            sequenceNumber,
            descriptionType,
            description.Trim(),
            amount,
            billingHeadCode,
            taxCode,
            categoryCode);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SequenceNumber;
        yield return DescriptionType;
        yield return Description;
        yield return Amount;
        yield return BillingHeadCode;
        yield return TaxCode;
        yield return CategoryCode;
    }
}
