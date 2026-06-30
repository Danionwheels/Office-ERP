using SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

namespace SafarSuite.ControlDesk.Application.Modules.SurveyValuation.SurveyJobEntry;

public sealed record SurveyJobEntryDto(
    Guid SurveyJobId,
    string SurveyJobNumber,
    SurveyJobIdentityDto Identity,
    SurveyJobDatesDto Dates,
    SurveyJobInsuredPartyDto? InsuredParty,
    SurveyJobAssignmentDto Assignment,
    SurveyJobVehicleDto Vehicle,
    SurveyJobPolicyLossDto PolicyLoss,
    SurveyJobInvoiceSummaryDto InvoiceSummary,
    SurveyJobSettlementDto Settlement,
    string? Remarks,
    SurveyMoneyDto InvoiceLineTotal,
    IReadOnlyCollection<SurveyDocumentChecklistItemDto> Documents,
    IReadOnlyCollection<SurveyJobInvoiceLineDto> InvoiceLines,
    DateTimeOffset CreatedAtUtc);

public sealed record SurveyJobIdentityDto(
    string SurveyTypeCode,
    string? ClientCode,
    string? ClientBranchCode,
    string? CompanyBranchCode,
    string? BillingBranchCode,
    SurveyPaymentMode PaymentMode,
    SurveyJobStatus Status,
    bool IsReInspection);

public sealed record SurveyJobDatesDto(
    DateOnly IntimationDate,
    DateOnly? DeliveredDate,
    DateOnly? ReInspectionDate,
    DateOnly? InvoiceDate,
    DateOnly? VoucherDate,
    DateOnly? DiscountDate,
    DateOnly? PurchaseOrderDate);

public sealed record SurveyJobInsuredPartyDto(
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? Cnic,
    string? ContactPerson,
    string? ContactDesignationCode,
    string? ReferenceNumber,
    string? CcNumber);

public sealed record SurveyJobAssignmentDto(
    string? SurveyorCode,
    string? SupervisorCode,
    string? ClaimTypeCode,
    string? RequestSourceCode,
    string? AreaCode,
    string? AgencyCode);

public sealed record SurveyJobVehicleDto(
    string? Make,
    string? RegistrationNumber,
    string? ChassisNumber,
    string? Model,
    string? EngineNumber,
    string? WorkshopCode);

public sealed record SurveyJobPolicyLossDto(
    string? LossNumber,
    string? PolicyNumber,
    string? PurchaseOrderNumber);

public sealed record SurveyJobInvoiceSummaryDto(
    string? InvoiceNumber,
    SurveyMoneyDto? NetAmount,
    SurveyMoneyDto? GrossAmount,
    SurveyMoneyDto? DiscountAmount,
    decimal? SalesTaxPercent,
    SurveyMoneyDto? SalesTaxAmount,
    SurveyPaymentMode PaymentMode,
    SurveyMoneyDto? WorkshopPaymentAmount,
    string? VoucherNumber,
    string? JournalCode,
    string? DiscountJournalCode);

public sealed record SurveyJobSettlementDto(
    SurveyMoneyDto? LossAmount,
    SurveyMoneyDto? SettledLaborAmount,
    SurveyMoneyDto? ApprovedPartsAmount,
    SurveyMoneyDto? PolicyDeductibleAmount,
    DateOnly? LaborBillDate,
    SurveyMoneyDto? LaborBillAmount,
    DateOnly? PartsBillDate,
    SurveyMoneyDto? PartsBillAmount,
    decimal? DepreciationPercent,
    SurveyMoneyDto? DepreciationAmount,
    decimal? SalvagePercent,
    SurveyMoneyDto? SalvageAmount);

public sealed record SurveyDocumentChecklistItemDto(
    SurveyDocumentType Type,
    SurveyDocumentStatus Status,
    DateOnly? ReceivedOn);

public sealed record SurveyJobInvoiceLineDto(
    int SequenceNumber,
    SurveyInvoiceLineDescriptionType DescriptionType,
    string Description,
    SurveyMoneyDto Amount,
    string? BillingHeadCode,
    string? TaxCode,
    string? CategoryCode);

public sealed record SurveyMoneyDto(decimal Amount, string CurrencyCode);
