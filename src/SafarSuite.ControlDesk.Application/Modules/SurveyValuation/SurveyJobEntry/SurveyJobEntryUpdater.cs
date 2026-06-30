using SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

namespace SafarSuite.ControlDesk.Application.Modules.SurveyValuation.SurveyJobEntry;

internal static class SurveyJobEntryUpdater
{
    public static void Apply(ISurveyJobEntryFields fields, SurveyJob surveyJob)
    {
        var surveyTypeCode = SurveyReferenceCode.Create(fields.SurveyTypeCode);

        surveyJob.UpdateIdentity(
            surveyTypeCode,
            SurveyReferenceCode.Optional(fields.ClientCode),
            SurveyReferenceCode.Optional(fields.ClientBranchCode),
            SurveyReferenceCode.Optional(fields.CompanyBranchCode),
            SurveyReferenceCode.Optional(fields.BillingBranchCode),
            fields.PaymentMode,
            fields.IsReInspection);

        surveyJob.UpdateDates(SurveyJobDates.Create(
            fields.IntimationDate,
            fields.DeliveredDate,
            fields.ReInspectionDate,
            fields.InvoiceDate,
            fields.VoucherDate,
            fields.DiscountDate,
            fields.PurchaseOrderDate));

        ApplyInsuredParty(fields, surveyJob);

        surveyJob.UpdateAssignment(SurveyAssignment.Create(
            SurveyReferenceCode.Optional(fields.SurveyorCode),
            SurveyReferenceCode.Optional(fields.SupervisorCode),
            SurveyReferenceCode.Optional(fields.ClaimTypeCode),
            SurveyReferenceCode.Optional(fields.RequestSourceCode),
            SurveyReferenceCode.Optional(fields.AreaCode),
            SurveyReferenceCode.Optional(fields.AgencyCode)));

        surveyJob.UpdateVehicle(VehicleDetails.Create(
            fields.VehicleMake,
            fields.VehicleRegistrationNumber,
            fields.VehicleChassisNumber,
            fields.VehicleModel,
            fields.VehicleEngineNumber,
            SurveyReferenceCode.Optional(fields.WorkshopCode)));

        surveyJob.UpdatePolicyLoss(PolicyLossDetails.Create(
            fields.LossNumber,
            fields.PolicyNumber,
            fields.PurchaseOrderNumber));

        surveyJob.UpdateRemarks(fields.Remarks);
    }

    private static void ApplyInsuredParty(ISurveyJobEntryFields fields, SurveyJob surveyJob)
    {
        if (!SurveyJobEntryValidationRules.HasInsuredDetails(fields))
        {
            surveyJob.ClearInsuredParty();
            return;
        }

        surveyJob.UpdateInsuredParty(InsuredParty.Create(
            fields.InsuredName!,
            fields.InsuredPhone,
            fields.InsuredEmail,
            fields.InsuredAddress,
            fields.InsuredCnic,
            fields.ContactPerson,
            SurveyReferenceCode.Optional(fields.ContactDesignationCode),
            fields.ReferenceNumber,
            fields.CcNumber));
    }
}
