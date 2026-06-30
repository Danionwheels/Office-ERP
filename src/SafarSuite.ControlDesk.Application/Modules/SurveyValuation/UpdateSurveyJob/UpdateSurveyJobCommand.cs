using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.SurveyJobEntry;
using SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

namespace SafarSuite.ControlDesk.Application.Modules.SurveyValuation.UpdateSurveyJob;

public sealed record UpdateSurveyJobCommand : ISurveyJobEntryFields
{
    public Guid SurveyJobId { get; init; }

    public required string SurveyTypeCode { get; init; }

    public SurveyJobStatus Status { get; init; } = SurveyJobStatus.Draft;

    public DateOnly IntimationDate { get; init; }

    public DateOnly? DeliveredDate { get; init; }

    public DateOnly? ReInspectionDate { get; init; }

    public DateOnly? InvoiceDate { get; init; }

    public DateOnly? VoucherDate { get; init; }

    public DateOnly? DiscountDate { get; init; }

    public DateOnly? PurchaseOrderDate { get; init; }

    public string? ClientCode { get; init; }

    public string? ClientBranchCode { get; init; }

    public string? CompanyBranchCode { get; init; }

    public string? BillingBranchCode { get; init; }

    public SurveyPaymentMode PaymentMode { get; init; } = SurveyPaymentMode.Unknown;

    public bool IsReInspection { get; init; }

    public string? InsuredName { get; init; }

    public string? InsuredPhone { get; init; }

    public string? InsuredEmail { get; init; }

    public string? InsuredAddress { get; init; }

    public string? InsuredCnic { get; init; }

    public string? ContactPerson { get; init; }

    public string? ContactDesignationCode { get; init; }

    public string? ReferenceNumber { get; init; }

    public string? CcNumber { get; init; }

    public string? SurveyorCode { get; init; }

    public string? SupervisorCode { get; init; }

    public string? ClaimTypeCode { get; init; }

    public string? RequestSourceCode { get; init; }

    public string? AreaCode { get; init; }

    public string? AgencyCode { get; init; }

    public string? VehicleMake { get; init; }

    public string? VehicleRegistrationNumber { get; init; }

    public string? VehicleChassisNumber { get; init; }

    public string? VehicleModel { get; init; }

    public string? VehicleEngineNumber { get; init; }

    public string? WorkshopCode { get; init; }

    public string? LossNumber { get; init; }

    public string? PolicyNumber { get; init; }

    public string? PurchaseOrderNumber { get; init; }

    public string? Remarks { get; init; }
}
