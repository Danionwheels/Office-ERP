using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.CreateSurveyJob;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.CreateSurveyJobBillingDraft;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.SurveyJobEntry;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.UpdateSurveyJobDocuments;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.UpdateSurveyJobInvoiceLines;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.UpdateSurveyJob;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.SurveyValuation;
using SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

namespace SafarSuite.ControlDesk.Api.Modules.SurveyValuation;

internal static class SurveyJobEntryApiMapper
{
    public static CreateSurveyJobCommand ToCommand(CreateSurveyJobRequest request)
    {
        var fields = request.Fields;

        return new CreateSurveyJobCommand
        {
            SurveyJobNumber = request.SurveyJobNumber,
            SurveyTypeCode = fields.SurveyTypeCode,
            IntimationDate = fields.IntimationDate,
            DeliveredDate = fields.DeliveredDate,
            ReInspectionDate = fields.ReInspectionDate,
            InvoiceDate = fields.InvoiceDate,
            VoucherDate = fields.VoucherDate,
            DiscountDate = fields.DiscountDate,
            PurchaseOrderDate = fields.PurchaseOrderDate,
            ClientCode = fields.ClientCode,
            ClientBranchCode = fields.ClientBranchCode,
            CompanyBranchCode = fields.CompanyBranchCode,
            BillingBranchCode = fields.BillingBranchCode,
            PaymentMode = ParseEnum(fields.PaymentMode, SurveyPaymentMode.Unknown),
            IsReInspection = fields.IsReInspection,
            InsuredName = fields.InsuredName,
            InsuredPhone = fields.InsuredPhone,
            InsuredEmail = fields.InsuredEmail,
            InsuredAddress = fields.InsuredAddress,
            InsuredCnic = fields.InsuredCnic,
            ContactPerson = fields.ContactPerson,
            ContactDesignationCode = fields.ContactDesignationCode,
            ReferenceNumber = fields.ReferenceNumber,
            CcNumber = fields.CcNumber,
            SurveyorCode = fields.SurveyorCode,
            SupervisorCode = fields.SupervisorCode,
            ClaimTypeCode = fields.ClaimTypeCode,
            RequestSourceCode = fields.RequestSourceCode,
            AreaCode = fields.AreaCode,
            AgencyCode = fields.AgencyCode,
            VehicleMake = fields.VehicleMake,
            VehicleRegistrationNumber = fields.VehicleRegistrationNumber,
            VehicleChassisNumber = fields.VehicleChassisNumber,
            VehicleModel = fields.VehicleModel,
            VehicleEngineNumber = fields.VehicleEngineNumber,
            WorkshopCode = fields.WorkshopCode,
            LossNumber = fields.LossNumber,
            PolicyNumber = fields.PolicyNumber,
            PurchaseOrderNumber = fields.PurchaseOrderNumber,
            Remarks = fields.Remarks
        };
    }

    public static UpdateSurveyJobCommand ToCommand(Guid surveyJobId, UpdateSurveyJobRequest request)
    {
        var fields = request.Fields;

        return new UpdateSurveyJobCommand
        {
            SurveyJobId = surveyJobId,
            SurveyTypeCode = fields.SurveyTypeCode,
            Status = ParseRequiredEnum<SurveyJobStatus>(request.Status),
            IntimationDate = fields.IntimationDate,
            DeliveredDate = fields.DeliveredDate,
            ReInspectionDate = fields.ReInspectionDate,
            InvoiceDate = fields.InvoiceDate,
            VoucherDate = fields.VoucherDate,
            DiscountDate = fields.DiscountDate,
            PurchaseOrderDate = fields.PurchaseOrderDate,
            ClientCode = fields.ClientCode,
            ClientBranchCode = fields.ClientBranchCode,
            CompanyBranchCode = fields.CompanyBranchCode,
            BillingBranchCode = fields.BillingBranchCode,
            PaymentMode = ParseEnum(fields.PaymentMode, SurveyPaymentMode.Unknown),
            IsReInspection = fields.IsReInspection,
            InsuredName = fields.InsuredName,
            InsuredPhone = fields.InsuredPhone,
            InsuredEmail = fields.InsuredEmail,
            InsuredAddress = fields.InsuredAddress,
            InsuredCnic = fields.InsuredCnic,
            ContactPerson = fields.ContactPerson,
            ContactDesignationCode = fields.ContactDesignationCode,
            ReferenceNumber = fields.ReferenceNumber,
            CcNumber = fields.CcNumber,
            SurveyorCode = fields.SurveyorCode,
            SupervisorCode = fields.SupervisorCode,
            ClaimTypeCode = fields.ClaimTypeCode,
            RequestSourceCode = fields.RequestSourceCode,
            AreaCode = fields.AreaCode,
            AgencyCode = fields.AgencyCode,
            VehicleMake = fields.VehicleMake,
            VehicleRegistrationNumber = fields.VehicleRegistrationNumber,
            VehicleChassisNumber = fields.VehicleChassisNumber,
            VehicleModel = fields.VehicleModel,
            VehicleEngineNumber = fields.VehicleEngineNumber,
            WorkshopCode = fields.WorkshopCode,
            LossNumber = fields.LossNumber,
            PolicyNumber = fields.PolicyNumber,
            PurchaseOrderNumber = fields.PurchaseOrderNumber,
            Remarks = fields.Remarks
        };
    }

    public static UpdateSurveyJobDocumentsCommand ToCommand(
        Guid surveyJobId,
        UpdateSurveyJobDocumentsRequest request)
    {
        return new UpdateSurveyJobDocumentsCommand(
            surveyJobId,
            request.Documents?
                .Select(document => new SurveyJobDocumentChecklistItemCommand(
                    ParseRequiredEnum<SurveyDocumentType>(document.Type),
                    ParseRequiredEnum<SurveyDocumentStatus>(document.Status),
                    document.ReceivedOn))
                .ToArray()!);
    }

    public static UpdateSurveyJobInvoiceLinesCommand ToCommand(
        Guid surveyJobId,
        UpdateSurveyJobInvoiceLinesRequest request)
    {
        return new UpdateSurveyJobInvoiceLinesCommand(
            surveyJobId,
            request.InvoiceLines?
                .Select(line => new SurveyJobInvoiceLineCommand(
                    line.SequenceNumber,
                    ParseRequiredEnum<SurveyInvoiceLineDescriptionType>(line.DescriptionType),
                    line.Description,
                    line.Amount,
                    line.CurrencyCode,
                    line.BillingHeadCode,
                    line.TaxCode,
                    line.CategoryCode))
                .ToArray());
    }

    public static CreateSurveyJobBillingDraftCommand ToCommand(
        Guid surveyJobId,
        CreateSurveyJobBillingDraftRequest request)
    {
        return new CreateSurveyJobBillingDraftCommand(
            surveyJobId,
            request.ClientId,
            request.ContractId,
            request.InvoiceNumber,
            request.IssueDate,
            request.DueDate,
            request.CurrencyCode);
    }

    public static CreateSurveyJobBillingDraftResponse ToResponse(CreateSurveyJobBillingDraftResult result)
    {
        return new CreateSurveyJobBillingDraftResponse(
            result.InvoiceId,
            result.InvoiceNumber,
            result.Status,
            result.TotalAmount,
            result.BalanceDue,
            result.CurrencyCode,
            result.Lines.Select(line => new CreateSurveyJobBillingDraftLineResponse(
                line.ChargeCodeId,
                line.ChargeCode,
                line.Description,
                line.Amount,
                line.CurrencyCode)).ToArray(),
            ToResponse(result.SurveyJob));
    }

    public static SurveyJobEntryResponse ToResponse(SurveyJobEntryDto dto)
    {
        return new SurveyJobEntryResponse(
            dto.SurveyJobId,
            dto.SurveyJobNumber,
            new SurveyJobIdentityResponse(
                dto.Identity.SurveyTypeCode,
                dto.Identity.ClientCode,
                dto.Identity.ClientBranchCode,
                dto.Identity.CompanyBranchCode,
                dto.Identity.BillingBranchCode,
                dto.Identity.PaymentMode.ToString(),
                dto.Identity.Status.ToString(),
                dto.Identity.IsReInspection),
            new SurveyJobDatesResponse(
                dto.Dates.IntimationDate,
                dto.Dates.DeliveredDate,
                dto.Dates.ReInspectionDate,
                dto.Dates.InvoiceDate,
                dto.Dates.VoucherDate,
                dto.Dates.DiscountDate,
                dto.Dates.PurchaseOrderDate),
            ToResponse(dto.InsuredParty),
            new SurveyJobAssignmentResponse(
                dto.Assignment.SurveyorCode,
                dto.Assignment.SupervisorCode,
                dto.Assignment.ClaimTypeCode,
                dto.Assignment.RequestSourceCode,
                dto.Assignment.AreaCode,
                dto.Assignment.AgencyCode),
            new SurveyJobVehicleResponse(
                dto.Vehicle.Make,
                dto.Vehicle.RegistrationNumber,
                dto.Vehicle.ChassisNumber,
                dto.Vehicle.Model,
                dto.Vehicle.EngineNumber,
                dto.Vehicle.WorkshopCode),
            new SurveyJobPolicyLossResponse(
                dto.PolicyLoss.LossNumber,
                dto.PolicyLoss.PolicyNumber,
                dto.PolicyLoss.PurchaseOrderNumber),
            ToResponse(dto.InvoiceSummary),
            ToResponse(dto.Settlement),
            dto.Remarks,
            ToResponse(dto.InvoiceLineTotal),
            dto.Documents.Select(ToResponse).ToArray(),
            dto.InvoiceLines.Select(ToResponse).ToArray(),
            dto.CreatedAtUtc);
    }

    private static SurveyJobInsuredPartyResponse? ToResponse(SurveyJobInsuredPartyDto? dto)
    {
        return dto is null
            ? null
            : new SurveyJobInsuredPartyResponse(
                dto.Name,
                dto.Phone,
                dto.Email,
                dto.Address,
                dto.Cnic,
                dto.ContactPerson,
                dto.ContactDesignationCode,
                dto.ReferenceNumber,
                dto.CcNumber);
    }

    private static SurveyJobInvoiceSummaryResponse ToResponse(SurveyJobInvoiceSummaryDto dto)
    {
        return new SurveyJobInvoiceSummaryResponse(
            dto.InvoiceNumber,
            ToNullableMoneyResponse(dto.NetAmount),
            ToNullableMoneyResponse(dto.GrossAmount),
            ToNullableMoneyResponse(dto.DiscountAmount),
            dto.SalesTaxPercent,
            ToNullableMoneyResponse(dto.SalesTaxAmount),
            dto.PaymentMode.ToString(),
            ToNullableMoneyResponse(dto.WorkshopPaymentAmount),
            dto.VoucherNumber,
            dto.JournalCode,
            dto.DiscountJournalCode);
    }

    private static SurveyJobSettlementResponse ToResponse(SurveyJobSettlementDto dto)
    {
        return new SurveyJobSettlementResponse(
            ToNullableMoneyResponse(dto.LossAmount),
            ToNullableMoneyResponse(dto.SettledLaborAmount),
            ToNullableMoneyResponse(dto.ApprovedPartsAmount),
            ToNullableMoneyResponse(dto.PolicyDeductibleAmount),
            dto.LaborBillDate,
            ToNullableMoneyResponse(dto.LaborBillAmount),
            dto.PartsBillDate,
            ToNullableMoneyResponse(dto.PartsBillAmount),
            dto.DepreciationPercent,
            ToNullableMoneyResponse(dto.DepreciationAmount),
            dto.SalvagePercent,
            ToNullableMoneyResponse(dto.SalvageAmount));
    }

    private static SurveyDocumentChecklistItemResponse ToResponse(SurveyDocumentChecklistItemDto dto)
    {
        return new SurveyDocumentChecklistItemResponse(
            dto.Type.ToString(),
            dto.Status.ToString(),
            dto.ReceivedOn);
    }

    private static SurveyJobInvoiceLineResponse ToResponse(SurveyJobInvoiceLineDto dto)
    {
        return new SurveyJobInvoiceLineResponse(
            dto.SequenceNumber,
            dto.DescriptionType.ToString(),
            dto.Description,
            ToResponse(dto.Amount),
            dto.BillingHeadCode,
            dto.TaxCode,
            dto.CategoryCode);
    }

    private static SurveyMoneyResponse ToResponse(SurveyMoneyDto dto)
    {
        return new SurveyMoneyResponse(dto.Amount, dto.CurrencyCode);
    }

    private static SurveyMoneyResponse? ToNullableMoneyResponse(SurveyMoneyDto? dto)
    {
        return dto is null ? null : ToResponse(dto);
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            ? parsed
            : InvalidEnum<TEnum>();
    }

    private static TEnum ParseRequiredEnum<TEnum>(string? value)
        where TEnum : struct, Enum
    {
        return string.IsNullOrWhiteSpace(value)
            ? InvalidEnum<TEnum>()
            : ParseEnum(value, InvalidEnum<TEnum>());
    }

    private static TEnum InvalidEnum<TEnum>()
        where TEnum : struct, Enum
    {
        return (TEnum)Enum.ToObject(typeof(TEnum), -1);
    }
}
