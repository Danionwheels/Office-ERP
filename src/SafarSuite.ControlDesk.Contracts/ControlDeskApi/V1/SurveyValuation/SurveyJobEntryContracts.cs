namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.SurveyValuation;

public sealed record CreateSurveyJobRequest(
    string SurveyJobNumber,
    SurveyJobEntryFieldsRequest Fields);

public sealed record CreateSurveyJobResponse(
    Guid SurveyJobId,
    string SurveyJobNumber,
    string Status);

public sealed record UpdateSurveyJobRequest(
    string Status,
    SurveyJobEntryFieldsRequest Fields);

public sealed record UpdateSurveyJobDocumentsRequest(
    IReadOnlyCollection<SurveyJobDocumentChecklistItemRequest>? Documents);

public sealed record SurveyJobDocumentChecklistItemRequest(
    string Type,
    string Status,
    DateOnly? ReceivedOn);

public sealed record UpdateSurveyJobInvoiceLinesRequest(
    IReadOnlyCollection<SurveyJobInvoiceLineRequest>? InvoiceLines);

public sealed record SurveyJobInvoiceLineRequest(
    int SequenceNumber,
    string DescriptionType,
    string Description,
    decimal Amount,
    string CurrencyCode,
    string? BillingHeadCode = null,
    string? TaxCode = null,
    string? CategoryCode = null);

public sealed record CreateSurveyJobBillingDraftRequest(
    Guid ClientId,
    Guid ContractId,
    string InvoiceNumber,
    DateOnly IssueDate,
    DateOnly DueDate,
    string CurrencyCode);

public sealed record CreateSurveyJobBillingDraftResponse(
    Guid InvoiceId,
    string InvoiceNumber,
    string Status,
    decimal TotalAmount,
    decimal BalanceDue,
    string CurrencyCode,
    IReadOnlyCollection<CreateSurveyJobBillingDraftLineResponse> Lines,
    SurveyJobEntryResponse SurveyJob);

public sealed record CreateSurveyJobBillingDraftLineResponse(
    Guid ChargeCodeId,
    string ChargeCode,
    string Description,
    decimal Amount,
    string CurrencyCode);

public sealed record SurveyJobEntryFieldsRequest(
    string SurveyTypeCode,
    DateOnly IntimationDate,
    DateOnly? DeliveredDate = null,
    DateOnly? ReInspectionDate = null,
    DateOnly? InvoiceDate = null,
    DateOnly? VoucherDate = null,
    DateOnly? DiscountDate = null,
    DateOnly? PurchaseOrderDate = null,
    string? ClientCode = null,
    string? ClientBranchCode = null,
    string? CompanyBranchCode = null,
    string? BillingBranchCode = null,
    string? PaymentMode = null,
    bool IsReInspection = false,
    string? InsuredName = null,
    string? InsuredPhone = null,
    string? InsuredEmail = null,
    string? InsuredAddress = null,
    string? InsuredCnic = null,
    string? ContactPerson = null,
    string? ContactDesignationCode = null,
    string? ReferenceNumber = null,
    string? CcNumber = null,
    string? SurveyorCode = null,
    string? SupervisorCode = null,
    string? ClaimTypeCode = null,
    string? RequestSourceCode = null,
    string? AreaCode = null,
    string? AgencyCode = null,
    string? VehicleMake = null,
    string? VehicleRegistrationNumber = null,
    string? VehicleChassisNumber = null,
    string? VehicleModel = null,
    string? VehicleEngineNumber = null,
    string? WorkshopCode = null,
    string? LossNumber = null,
    string? PolicyNumber = null,
    string? PurchaseOrderNumber = null,
    string? Remarks = null);

public sealed record SurveyJobEntryResponse(
    Guid SurveyJobId,
    string SurveyJobNumber,
    SurveyJobIdentityResponse Identity,
    SurveyJobDatesResponse Dates,
    SurveyJobInsuredPartyResponse? InsuredParty,
    SurveyJobAssignmentResponse Assignment,
    SurveyJobVehicleResponse Vehicle,
    SurveyJobPolicyLossResponse PolicyLoss,
    SurveyJobInvoiceSummaryResponse InvoiceSummary,
    SurveyJobSettlementResponse Settlement,
    string? Remarks,
    SurveyMoneyResponse InvoiceLineTotal,
    IReadOnlyCollection<SurveyDocumentChecklistItemResponse> Documents,
    IReadOnlyCollection<SurveyJobInvoiceLineResponse> InvoiceLines,
    DateTimeOffset CreatedAtUtc);

public sealed record SurveyJobIdentityResponse(
    string SurveyTypeCode,
    string? ClientCode,
    string? ClientBranchCode,
    string? CompanyBranchCode,
    string? BillingBranchCode,
    string PaymentMode,
    string Status,
    bool IsReInspection);

public sealed record SurveyJobDatesResponse(
    DateOnly IntimationDate,
    DateOnly? DeliveredDate,
    DateOnly? ReInspectionDate,
    DateOnly? InvoiceDate,
    DateOnly? VoucherDate,
    DateOnly? DiscountDate,
    DateOnly? PurchaseOrderDate);

public sealed record SurveyJobInsuredPartyResponse(
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? Cnic,
    string? ContactPerson,
    string? ContactDesignationCode,
    string? ReferenceNumber,
    string? CcNumber);

public sealed record SurveyJobAssignmentResponse(
    string? SurveyorCode,
    string? SupervisorCode,
    string? ClaimTypeCode,
    string? RequestSourceCode,
    string? AreaCode,
    string? AgencyCode);

public sealed record SurveyJobVehicleResponse(
    string? Make,
    string? RegistrationNumber,
    string? ChassisNumber,
    string? Model,
    string? EngineNumber,
    string? WorkshopCode);

public sealed record SurveyJobPolicyLossResponse(
    string? LossNumber,
    string? PolicyNumber,
    string? PurchaseOrderNumber);

public sealed record SurveyJobInvoiceSummaryResponse(
    string? InvoiceNumber,
    SurveyMoneyResponse? NetAmount,
    SurveyMoneyResponse? GrossAmount,
    SurveyMoneyResponse? DiscountAmount,
    decimal? SalesTaxPercent,
    SurveyMoneyResponse? SalesTaxAmount,
    string PaymentMode,
    SurveyMoneyResponse? WorkshopPaymentAmount,
    string? VoucherNumber,
    string? JournalCode,
    string? DiscountJournalCode);

public sealed record SurveyJobSettlementResponse(
    SurveyMoneyResponse? LossAmount,
    SurveyMoneyResponse? SettledLaborAmount,
    SurveyMoneyResponse? ApprovedPartsAmount,
    SurveyMoneyResponse? PolicyDeductibleAmount,
    DateOnly? LaborBillDate,
    SurveyMoneyResponse? LaborBillAmount,
    DateOnly? PartsBillDate,
    SurveyMoneyResponse? PartsBillAmount,
    decimal? DepreciationPercent,
    SurveyMoneyResponse? DepreciationAmount,
    decimal? SalvagePercent,
    SurveyMoneyResponse? SalvageAmount);

public sealed record SurveyDocumentChecklistItemResponse(
    string Type,
    string Status,
    DateOnly? ReceivedOn);

public sealed record SurveyJobInvoiceLineResponse(
    int SequenceNumber,
    string DescriptionType,
    string Description,
    SurveyMoneyResponse Amount,
    string? BillingHeadCode,
    string? TaxCode,
    string? CategoryCode);

public sealed record SurveyMoneyResponse(decimal Amount, string CurrencyCode);
