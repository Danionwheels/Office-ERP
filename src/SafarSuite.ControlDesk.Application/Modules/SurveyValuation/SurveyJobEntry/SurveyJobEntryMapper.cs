using SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Application.Modules.SurveyValuation.SurveyJobEntry;

internal static class SurveyJobEntryMapper
{
    public static SurveyJobEntryDto ToDto(SurveyJob surveyJob)
    {
        return new SurveyJobEntryDto(
            surveyJob.Id.Value,
            surveyJob.Number.Value,
            new SurveyJobIdentityDto(
                surveyJob.SurveyTypeCode.Value,
                Code(surveyJob.ClientCode),
                Code(surveyJob.ClientBranchCode),
                Code(surveyJob.CompanyBranchCode),
                Code(surveyJob.BillingBranchCode),
                surveyJob.PaymentMode,
                surveyJob.Status,
                surveyJob.IsReInspection),
            new SurveyJobDatesDto(
                surveyJob.Dates.IntimationDate,
                surveyJob.Dates.DeliveredDate,
                surveyJob.Dates.ReInspectionDate,
                surveyJob.Dates.InvoiceDate,
                surveyJob.Dates.VoucherDate,
                surveyJob.Dates.DiscountDate,
                surveyJob.Dates.PurchaseOrderDate),
            ToInsuredPartyDto(surveyJob.InsuredParty),
            new SurveyJobAssignmentDto(
                Code(surveyJob.Assignment.SurveyorCode),
                Code(surveyJob.Assignment.SupervisorCode),
                Code(surveyJob.Assignment.ClaimTypeCode),
                Code(surveyJob.Assignment.RequestSourceCode),
                Code(surveyJob.Assignment.AreaCode),
                Code(surveyJob.Assignment.AgencyCode)),
            new SurveyJobVehicleDto(
                surveyJob.Vehicle.Make,
                surveyJob.Vehicle.RegistrationNumber,
                surveyJob.Vehicle.ChassisNumber,
                surveyJob.Vehicle.Model,
                surveyJob.Vehicle.EngineNumber,
                Code(surveyJob.Vehicle.WorkshopCode)),
            new SurveyJobPolicyLossDto(
                surveyJob.PolicyLoss.LossNumber,
                surveyJob.PolicyLoss.PolicyNumber,
                surveyJob.PolicyLoss.PurchaseOrderNumber),
            ToInvoiceSummaryDto(surveyJob.InvoiceSummary),
            ToSettlementDto(surveyJob.Settlement),
            surveyJob.Remarks,
            ToMoneyDto(surveyJob.InvoiceLineTotal),
            surveyJob.Documents
                .OrderBy(document => document.Type)
                .Select(ToDocumentDto)
                .ToArray(),
            surveyJob.InvoiceLines
                .OrderBy(line => line.SequenceNumber)
                .Select(ToInvoiceLineDto)
                .ToArray(),
            surveyJob.CreatedAtUtc);
    }

    private static SurveyJobInsuredPartyDto? ToInsuredPartyDto(InsuredParty? insuredParty)
    {
        return insuredParty is null
            ? null
            : new SurveyJobInsuredPartyDto(
                insuredParty.Name,
                insuredParty.Phone,
                insuredParty.Email,
                insuredParty.Address,
                insuredParty.Cnic,
                insuredParty.ContactPerson,
                Code(insuredParty.ContactDesignationCode),
                insuredParty.ReferenceNumber,
                insuredParty.CcNumber);
    }

    private static SurveyJobInvoiceSummaryDto ToInvoiceSummaryDto(SurveyJobInvoiceSummary invoiceSummary)
    {
        return new SurveyJobInvoiceSummaryDto(
            invoiceSummary.InvoiceNumber,
            ToNullableMoneyDto(invoiceSummary.NetAmount),
            ToNullableMoneyDto(invoiceSummary.GrossAmount),
            ToNullableMoneyDto(invoiceSummary.DiscountAmount),
            invoiceSummary.SalesTaxPercent,
            ToNullableMoneyDto(invoiceSummary.SalesTaxAmount),
            invoiceSummary.PaymentMode,
            ToNullableMoneyDto(invoiceSummary.WorkshopPaymentAmount),
            invoiceSummary.VoucherNumber,
            Code(invoiceSummary.JournalCode),
            Code(invoiceSummary.DiscountJournalCode));
    }

    private static SurveyJobSettlementDto ToSettlementDto(SurveyJobSettlement settlement)
    {
        return new SurveyJobSettlementDto(
            ToNullableMoneyDto(settlement.LossAmount),
            ToNullableMoneyDto(settlement.SettledLaborAmount),
            ToNullableMoneyDto(settlement.ApprovedPartsAmount),
            ToNullableMoneyDto(settlement.PolicyDeductibleAmount),
            settlement.LaborBillDate,
            ToNullableMoneyDto(settlement.LaborBillAmount),
            settlement.PartsBillDate,
            ToNullableMoneyDto(settlement.PartsBillAmount),
            settlement.DepreciationPercent,
            ToNullableMoneyDto(settlement.DepreciationAmount),
            settlement.SalvagePercent,
            ToNullableMoneyDto(settlement.SalvageAmount));
    }

    private static SurveyDocumentChecklistItemDto ToDocumentDto(SurveyDocumentChecklistItem document)
    {
        return new SurveyDocumentChecklistItemDto(document.Type, document.Status, document.ReceivedOn);
    }

    private static SurveyJobInvoiceLineDto ToInvoiceLineDto(SurveyJobInvoiceLine line)
    {
        return new SurveyJobInvoiceLineDto(
            line.SequenceNumber,
            line.DescriptionType,
            line.Description,
            ToMoneyDto(line.Amount),
            Code(line.BillingHeadCode),
            Code(line.TaxCode),
            Code(line.CategoryCode));
    }

    private static SurveyMoneyDto ToMoneyDto(Money money)
    {
        return new SurveyMoneyDto(money.Amount, money.CurrencyCode);
    }

    private static SurveyMoneyDto? ToNullableMoneyDto(Money? money)
    {
        return money is null ? null : ToMoneyDto(money);
    }

    private static string? Code(SurveyReferenceCode? referenceCode)
    {
        return referenceCode?.Value;
    }
}
