using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

public sealed class SurveyJobDates : ValueObject
{
    private SurveyJobDates(
        DateOnly intimationDate,
        DateOnly? deliveredDate,
        DateOnly? reInspectionDate,
        DateOnly? invoiceDate,
        DateOnly? voucherDate,
        DateOnly? discountDate,
        DateOnly? purchaseOrderDate)
    {
        IntimationDate = intimationDate;
        DeliveredDate = deliveredDate;
        ReInspectionDate = reInspectionDate;
        InvoiceDate = invoiceDate;
        VoucherDate = voucherDate;
        DiscountDate = discountDate;
        PurchaseOrderDate = purchaseOrderDate;
    }

    public DateOnly IntimationDate { get; }

    public DateOnly? DeliveredDate { get; }

    public DateOnly? ReInspectionDate { get; }

    public DateOnly? InvoiceDate { get; }

    public DateOnly? VoucherDate { get; }

    public DateOnly? DiscountDate { get; }

    public DateOnly? PurchaseOrderDate { get; }

    public static SurveyJobDates Create(
        DateOnly intimationDate,
        DateOnly? deliveredDate = null,
        DateOnly? reInspectionDate = null,
        DateOnly? invoiceDate = null,
        DateOnly? voucherDate = null,
        DateOnly? discountDate = null,
        DateOnly? purchaseOrderDate = null)
    {
        if (deliveredDate.HasValue && deliveredDate.Value < intimationDate)
        {
            throw new ArgumentException("Delivered date cannot be before intimation date.", nameof(deliveredDate));
        }

        return new SurveyJobDates(
            intimationDate,
            deliveredDate,
            reInspectionDate,
            invoiceDate,
            voucherDate,
            discountDate,
            purchaseOrderDate);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return IntimationDate;
        yield return DeliveredDate;
        yield return ReInspectionDate;
        yield return InvoiceDate;
        yield return VoucherDate;
        yield return DiscountDate;
        yield return PurchaseOrderDate;
    }
}
