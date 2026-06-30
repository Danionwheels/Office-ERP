import type {
  SurveyJobEntry,
  SurveyJobEntryFields,
  SurveyJobStatus
} from "../types/surveyJobEntryTypes";

export function fieldsFromSurveyJobEntry(entry: SurveyJobEntry): SurveyJobEntryFields {
  return {
    surveyTypeCode: entry.identity.surveyTypeCode,
    intimationDate: entry.dates.intimationDate,
    deliveredDate: entry.dates.deliveredDate ?? "",
    reInspectionDate: entry.dates.reInspectionDate ?? "",
    invoiceDate: entry.dates.invoiceDate ?? "",
    voucherDate: entry.dates.voucherDate ?? "",
    discountDate: entry.dates.discountDate ?? "",
    purchaseOrderDate: entry.dates.purchaseOrderDate ?? "",
    clientCode: entry.identity.clientCode ?? "",
    clientBranchCode: entry.identity.clientBranchCode ?? "",
    companyBranchCode: entry.identity.companyBranchCode ?? "",
    billingBranchCode: entry.identity.billingBranchCode ?? "",
    paymentMode: entry.identity.paymentMode,
    isReInspection: entry.identity.isReInspection,
    insuredName: entry.insuredParty?.name ?? "",
    insuredPhone: entry.insuredParty?.phone ?? "",
    insuredEmail: entry.insuredParty?.email ?? "",
    insuredAddress: entry.insuredParty?.address ?? "",
    insuredCnic: entry.insuredParty?.cnic ?? "",
    contactPerson: entry.insuredParty?.contactPerson ?? "",
    contactDesignationCode: entry.insuredParty?.contactDesignationCode ?? "",
    referenceNumber: entry.insuredParty?.referenceNumber ?? "",
    ccNumber: entry.insuredParty?.ccNumber ?? "",
    surveyorCode: entry.assignment.surveyorCode ?? "",
    supervisorCode: entry.assignment.supervisorCode ?? "",
    claimTypeCode: entry.assignment.claimTypeCode ?? "",
    requestSourceCode: entry.assignment.requestSourceCode ?? "",
    areaCode: entry.assignment.areaCode ?? "",
    agencyCode: entry.assignment.agencyCode ?? "",
    vehicleMake: entry.vehicle.make ?? "",
    vehicleRegistrationNumber: entry.vehicle.registrationNumber ?? "",
    vehicleChassisNumber: entry.vehicle.chassisNumber ?? "",
    vehicleModel: entry.vehicle.model ?? "",
    vehicleEngineNumber: entry.vehicle.engineNumber ?? "",
    workshopCode: entry.vehicle.workshopCode ?? "",
    lossNumber: entry.policyLoss.lossNumber ?? "",
    policyNumber: entry.policyLoss.policyNumber ?? "",
    purchaseOrderNumber: entry.policyLoss.purchaseOrderNumber ?? "",
    remarks: entry.remarks ?? ""
  };
}

export function statusFromSurveyJobEntry(entry: SurveyJobEntry): SurveyJobStatus {
  return entry.identity.status;
}
