using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

public sealed class SurveyJob : Entity<SurveyJobId>
{
    private readonly List<SurveyDocumentChecklistItem> _documents = [];
    private readonly List<SurveyJobInvoiceLine> _invoiceLines = [];

    private SurveyJob(
        SurveyJobId id,
        SurveyJobNumber number,
        SurveyReferenceCode surveyTypeCode,
        SurveyJobDates dates,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        Number = number;
        SurveyTypeCode = surveyTypeCode;
        Dates = dates;
        CreatedAtUtc = createdAtUtc;
        Status = SurveyJobStatus.Draft;
        PaymentMode = SurveyPaymentMode.Unknown;
        Assignment = SurveyAssignment.Create();
        Vehicle = VehicleDetails.Create();
        PolicyLoss = PolicyLossDetails.Create();
        InvoiceSummary = SurveyJobInvoiceSummary.Create();
        Settlement = SurveyJobSettlement.Create();
    }

    public SurveyJobNumber Number { get; private set; }

    public SurveyReferenceCode SurveyTypeCode { get; private set; }

    public SurveyReferenceCode? ClientCode { get; private set; }

    public SurveyReferenceCode? ClientBranchCode { get; private set; }

    public SurveyReferenceCode? CompanyBranchCode { get; private set; }

    public SurveyReferenceCode? BillingBranchCode { get; private set; }

    public SurveyJobStatus Status { get; private set; }

    public SurveyPaymentMode PaymentMode { get; private set; }

    public bool IsReInspection { get; private set; }

    public SurveyJobDates Dates { get; private set; }

    public InsuredParty? InsuredParty { get; private set; }

    public SurveyAssignment Assignment { get; private set; }

    public VehicleDetails Vehicle { get; private set; }

    public PolicyLossDetails PolicyLoss { get; private set; }

    public SurveyJobInvoiceSummary InvoiceSummary { get; private set; }

    public SurveyJobSettlement Settlement { get; private set; }

    public string? Remarks { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public IReadOnlyCollection<SurveyDocumentChecklistItem> Documents => _documents.AsReadOnly();

    public IReadOnlyCollection<SurveyJobInvoiceLine> InvoiceLines => _invoiceLines.AsReadOnly();

    public Money InvoiceLineTotal => _invoiceLines.Count == 0
        ? Money.Pkr(0)
        : _invoiceLines.Aggregate(
            Money.Zero(_invoiceLines[0].Amount.CurrencyCode),
            (total, line) => total.Add(line.Amount));

    public static SurveyJob Create(
        SurveyJobId id,
        SurveyJobNumber number,
        SurveyReferenceCode surveyTypeCode,
        SurveyJobDates dates,
        DateTimeOffset createdAtUtc)
    {
        return new SurveyJob(id, number, surveyTypeCode, dates, createdAtUtc);
    }

    public void UpdateIdentity(
        SurveyReferenceCode surveyTypeCode,
        SurveyReferenceCode? clientCode,
        SurveyReferenceCode? clientBranchCode,
        SurveyReferenceCode? companyBranchCode,
        SurveyReferenceCode? billingBranchCode,
        SurveyPaymentMode paymentMode,
        bool isReInspection)
    {
        SurveyTypeCode = surveyTypeCode;
        ClientCode = clientCode;
        ClientBranchCode = clientBranchCode;
        CompanyBranchCode = companyBranchCode;
        BillingBranchCode = billingBranchCode;
        PaymentMode = paymentMode;
        IsReInspection = isReInspection;
    }

    public void UpdateDates(SurveyJobDates dates)
    {
        Dates = dates;
    }

    public void UpdateInsuredParty(InsuredParty insuredParty)
    {
        InsuredParty = insuredParty;
    }

    public void ClearInsuredParty()
    {
        InsuredParty = null;
    }

    public void UpdateAssignment(SurveyAssignment assignment)
    {
        Assignment = assignment;
    }

    public void UpdateVehicle(VehicleDetails vehicle)
    {
        Vehicle = vehicle;
    }

    public void UpdatePolicyLoss(PolicyLossDetails policyLoss)
    {
        PolicyLoss = policyLoss;
    }

    public void UpdateInvoiceSummary(SurveyJobInvoiceSummary invoiceSummary)
    {
        InvoiceSummary = invoiceSummary;
    }

    public void UpdateSettlement(SurveyJobSettlement settlement)
    {
        Settlement = settlement;
    }

    public void UpdateRemarks(string? remarks)
    {
        Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();
    }

    public void SetStatus(SurveyJobStatus status)
    {
        Status = status;
    }

    public void MarkDelivered(DateOnly deliveredDate)
    {
        Dates = SurveyJobDates.Create(
            Dates.IntimationDate,
            deliveredDate,
            Dates.ReInspectionDate,
            Dates.InvoiceDate,
            Dates.VoucherDate,
            Dates.DiscountDate,
            Dates.PurchaseOrderDate);

        Status = SurveyJobStatus.Delivered;
    }

    public void Cancel()
    {
        Status = SurveyJobStatus.Cancelled;
    }

    public void SetDocument(SurveyDocumentChecklistItem document)
    {
        _documents.RemoveAll(existing => existing.Type == document.Type);
        _documents.Add(document);
    }

    public void ReplaceDocuments(IEnumerable<SurveyDocumentChecklistItem> documents)
    {
        _documents.Clear();
        _documents.AddRange(documents.OrderBy(document => document.Type));
    }

    public void UpsertInvoiceLine(SurveyJobInvoiceLine line)
    {
        _invoiceLines.RemoveAll(existing => existing.SequenceNumber == line.SequenceNumber);
        _invoiceLines.Add(line);
        _invoiceLines.Sort((left, right) => left.SequenceNumber.CompareTo(right.SequenceNumber));
    }

    public void ReplaceInvoiceLines(IEnumerable<SurveyJobInvoiceLine> lines)
    {
        _invoiceLines.Clear();

        foreach (var line in lines.OrderBy(line => line.SequenceNumber))
        {
            UpsertInvoiceLine(line);
        }
    }

    public void RemoveInvoiceLine(int sequenceNumber)
    {
        if (sequenceNumber <= 0)
        {
            throw new ArgumentException("Sequence number must be positive.", nameof(sequenceNumber));
        }

        _invoiceLines.RemoveAll(existing => existing.SequenceNumber == sequenceNumber);
    }
}
