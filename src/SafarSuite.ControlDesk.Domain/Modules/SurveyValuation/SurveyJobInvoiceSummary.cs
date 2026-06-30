using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

public sealed class SurveyJobInvoiceSummary : ValueObject
{
    private SurveyJobInvoiceSummary(
        string? invoiceNumber,
        Money? netAmount,
        Money? grossAmount,
        Money? discountAmount,
        decimal? salesTaxPercent,
        Money? salesTaxAmount,
        SurveyPaymentMode paymentMode,
        Money? workshopPaymentAmount,
        string? voucherNumber,
        SurveyReferenceCode? journalCode,
        SurveyReferenceCode? discountJournalCode)
    {
        InvoiceNumber = invoiceNumber;
        NetAmount = netAmount;
        GrossAmount = grossAmount;
        DiscountAmount = discountAmount;
        SalesTaxPercent = salesTaxPercent;
        SalesTaxAmount = salesTaxAmount;
        PaymentMode = paymentMode;
        WorkshopPaymentAmount = workshopPaymentAmount;
        VoucherNumber = voucherNumber;
        JournalCode = journalCode;
        DiscountJournalCode = discountJournalCode;
    }

    public string? InvoiceNumber { get; }

    public Money? NetAmount { get; }

    public Money? GrossAmount { get; }

    public Money? DiscountAmount { get; }

    public decimal? SalesTaxPercent { get; }

    public Money? SalesTaxAmount { get; }

    public SurveyPaymentMode PaymentMode { get; }

    public Money? WorkshopPaymentAmount { get; }

    public string? VoucherNumber { get; }

    public SurveyReferenceCode? JournalCode { get; }

    public SurveyReferenceCode? DiscountJournalCode { get; }

    public static SurveyJobInvoiceSummary Create(
        string? invoiceNumber = null,
        Money? netAmount = null,
        Money? grossAmount = null,
        Money? discountAmount = null,
        decimal? salesTaxPercent = null,
        Money? salesTaxAmount = null,
        SurveyPaymentMode paymentMode = SurveyPaymentMode.Unknown,
        Money? workshopPaymentAmount = null,
        string? voucherNumber = null,
        SurveyReferenceCode? journalCode = null,
        SurveyReferenceCode? discountJournalCode = null)
    {
        if (salesTaxPercent is < 0)
        {
            throw new ArgumentException("Sales tax percent cannot be negative.", nameof(salesTaxPercent));
        }

        return new SurveyJobInvoiceSummary(
            Clean(invoiceNumber),
            netAmount,
            grossAmount,
            discountAmount,
            salesTaxPercent,
            salesTaxAmount,
            paymentMode,
            workshopPaymentAmount,
            Clean(voucherNumber),
            journalCode,
            discountJournalCode);
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return InvoiceNumber;
        yield return NetAmount;
        yield return GrossAmount;
        yield return DiscountAmount;
        yield return SalesTaxPercent;
        yield return SalesTaxAmount;
        yield return PaymentMode;
        yield return WorkshopPaymentAmount;
        yield return VoucherNumber;
        yield return JournalCode;
        yield return DiscountJournalCode;
    }
}
