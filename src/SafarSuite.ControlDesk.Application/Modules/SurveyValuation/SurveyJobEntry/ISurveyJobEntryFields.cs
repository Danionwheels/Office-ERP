using SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

namespace SafarSuite.ControlDesk.Application.Modules.SurveyValuation.SurveyJobEntry;

public interface ISurveyJobEntryFields
{
    string SurveyTypeCode { get; }

    DateOnly IntimationDate { get; }

    DateOnly? DeliveredDate { get; }

    DateOnly? ReInspectionDate { get; }

    DateOnly? InvoiceDate { get; }

    DateOnly? VoucherDate { get; }

    DateOnly? DiscountDate { get; }

    DateOnly? PurchaseOrderDate { get; }

    string? ClientCode { get; }

    string? ClientBranchCode { get; }

    string? CompanyBranchCode { get; }

    string? BillingBranchCode { get; }

    SurveyPaymentMode PaymentMode { get; }

    bool IsReInspection { get; }

    string? InsuredName { get; }

    string? InsuredPhone { get; }

    string? InsuredEmail { get; }

    string? InsuredAddress { get; }

    string? InsuredCnic { get; }

    string? ContactPerson { get; }

    string? ContactDesignationCode { get; }

    string? ReferenceNumber { get; }

    string? CcNumber { get; }

    string? SurveyorCode { get; }

    string? SupervisorCode { get; }

    string? ClaimTypeCode { get; }

    string? RequestSourceCode { get; }

    string? AreaCode { get; }

    string? AgencyCode { get; }

    string? VehicleMake { get; }

    string? VehicleRegistrationNumber { get; }

    string? VehicleChassisNumber { get; }

    string? VehicleModel { get; }

    string? VehicleEngineNumber { get; }

    string? WorkshopCode { get; }

    string? LossNumber { get; }

    string? PolicyNumber { get; }

    string? PurchaseOrderNumber { get; }

    string? Remarks { get; }
}
